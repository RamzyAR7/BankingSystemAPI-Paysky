using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.AuthorizationServices
{
    public class TransactionAuthorizationService : ITransactionAuthorizationService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<TransactionAuthorizationService> _logger;

        public TransactionAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver,
            ILogger<TransactionAuthorizationService> logger)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
            _logger = logger;
        }

        public async Task<Result> CanInitiateTransferAsync(int sourceAccountId, int targetAccountId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var sourceAccountResult = await LoadSourceAccountAsync(sourceAccountId);
            if (sourceAccountResult.IsFailure)
                return Result.Failure(sourceAccountResult.Errors);

            var authorizationResult = await ValidateTransferAuthorizationAsync(sourceAccountResult.Value!, scopeResult.Value);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Transfer authorization granted: SourceAccount={SourceId}, TargetAccount={TargetId}, Scope={Scope}", 
                        sourceAccountId, targetAccountId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Transfer authorization denied: SourceAccount={SourceId}, TargetAccount={TargetId}, UserId={UserId}, Errors={Errors}",
                        sourceAccountId, targetAccountId, _currentUser.UserId, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> FilterTransactionsAsync(
            IQueryable<Transaction> query, int pageNumber = 1, int pageSize = 10)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Failure(scopeResult.Errors);

            var queryResult = await ApplyScopeFilteringAsync(query, scopeResult.Value, pageNumber, pageSize);
            
            // Add side effects using ResultExtensions
            queryResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Transaction filtering completed: Count={Count}, Page={Page}, Size={Size}, Scope={Scope}", 
                        queryResult.Value.TotalCount, pageNumber, pageSize, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Transaction filtering failed: UserId={UserId}, Scope={Scope}, Errors={Errors}",
                        _currentUser.UserId, scopeResult.Value, string.Join(", ", errors));
                });

            return queryResult;
        }

        private async Task<Result<AccessScope>> GetScopeAsync()
        {
            try
            {
                var scope = await _scopeResolver.GetScopeAsync();
                return Result<AccessScope>.Success(scope);
            }
            catch (Exception ex)
            {
                return Result<AccessScope>.BadRequest($"Failed to resolve access scope: {ex.Message}");
            }
        }

        private async Task<Result<Account>> LoadSourceAccountAsync(int sourceAccountId)
        {
            var srcSpec = new BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification(sourceAccountId);
            var source = await _uow.AccountRepository.FindAsync(srcSpec);
            return source.ToResult($"Source account with ID '{sourceAccountId}' not found.");
        }

        private async Task<Result> ValidateTransferAuthorizationAsync(Account source, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => ValidateSelfTransferAuthorization(source),
                AccessScope.BankLevel => await ValidateBankLevelTransferAuthorizationAsync(source),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private Result ValidateSelfTransferAuthorization(Account source)
        {
            // Acting user must be source owner
            return _currentUser.UserId.Equals(source.UserId, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.Forbidden("Clients cannot initiate transfers from accounts they don't own.");
        }

        private async Task<Result> ValidateBankLevelTransferAuthorizationAsync(Account source)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            var roleValidationResult = await ValidateSourceAccountOwnerRoleAsync(source.UserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            var bankValidationResult = BankGuard.ValidateSameBank(actingUserResult.Value!.BankId, source.User.BankId);
            return bankValidationResult;
        }

        private async Task<Result<ApplicationUser>> GetActingUserAsync()
        {
            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            return actingUser.ToResult("Acting user not found.");
        }

        private async Task<Result> ValidateSourceAccountOwnerRoleAsync(string sourceUserId)
        {
            var sourceOwnerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(sourceUserId);
            return RoleHelper.IsClient(sourceOwnerRole?.Name)
                ? Result.Success()
                : Result.Forbidden("Transfers can only be initiated from Client-owned accounts.");
        }

        private async Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> ApplyScopeFilteringAsync(
            IQueryable<Transaction> query, AccessScope scope, int pageNumber, int pageSize)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;
                var filteredQuery = await ApplyScopeFilterAsync(query, scope);
                
                if (filteredQuery.IsFailure)
                    return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Failure(filteredQuery.Errors);

                var baseQuery = filteredQuery.Value!.OrderByDescending(t => t.Timestamp);
                var total = await baseQuery.CountAsync();
                var items = await baseQuery.Skip(skip).Take(pageSize).ToListAsync();

                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((items, total));
            }
            catch (Exception ex)
            {
                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.BadRequest($"Failed to filter transactions: {ex.Message}");
            }
        }

        private async Task<Result<IQueryable<Transaction>>> ApplyScopeFilterAsync(IQueryable<Transaction> query, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result<IQueryable<Transaction>>.Success(query),
                AccessScope.Self => Result<IQueryable<Transaction>>.Success(
                    query.Where(t => t.AccountTransactions.Any(at => at.Account.UserId == _currentUser.UserId))),
                AccessScope.BankLevel => await ApplyBankLevelFilterAsync(query),
                _ => Result<IQueryable<Transaction>>.Success(query.Where(_ => false)) // No results for unknown scope
            };
        }

        private async Task<Result<IQueryable<Transaction>>> ApplyBankLevelFilterAsync(IQueryable<Transaction> query)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return Result<IQueryable<Transaction>>.Success(query.Where(_ => false)); // No results if can't get user

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            var filteredQuery = query.Where(t => 
                t.AccountTransactions.Any(at => 
                    clientUserIds.Contains(at.Account.UserId) && 
                    at.Account.User.BankId == actingUserResult.Value!.BankId));

            return Result<IQueryable<Transaction>>.Success(filteredQuery);
        }
    }
}
