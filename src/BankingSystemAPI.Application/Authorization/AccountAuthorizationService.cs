using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Specifications.UserSpecifications;

namespace BankingSystemAPI.Application.AuthorizationServices
{
    public class AccountAuthorizationService : IAccountAuthorizationService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<AccountAuthorizationService> _logger;

        public AccountAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver,
            ILogger<AccountAuthorizationService> logger)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
            _logger = logger;
        }

        public async Task<Result> CanViewAccountAsync(int accountId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var accountResult = await LoadAccountAsync(accountId);
            if (accountResult.IsFailure)
                return Result.Failure(accountResult.Errors);

            var authorizationResult = await ValidateViewAccountAuthorizationAsync(accountResult.Value!, scopeResult.Value);
            
                authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Account view authorization granted: AccountId={AccountId}, UserId={UserId}, Scope={Scope}", 
                        accountId, _currentUser.UserId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Account view authorization denied: AccountId={AccountId}, UserId={UserId}, Errors={Errors}",
                        accountId, _currentUser.UserId, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result> CanModifyAccountAsync(int accountId, AccountModificationOperation operation)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            var accountResult = await LoadAccountAsync(accountId);
            if (accountResult.IsFailure)
                return Result.Failure(accountResult.Errors);

            var selfModificationResult = ValidateSelfModificationRules(actingUserResult.Value!, accountResult.Value!, operation);
            if (selfModificationResult.IsFailure)
                return selfModificationResult;

            var authorizationResult = await ValidateAccountModificationAuthorizationAsync(accountResult.Value!, operation, scopeResult.Value, actingUserResult.Value!);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Account modification authorization granted: AccountId={AccountId}, Operation={Operation}, UserId={UserId}, Scope={Scope}", 
                        accountId, operation, _currentUser.UserId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Account modification authorization denied: AccountId={AccountId}, Operation={Operation}, UserId={UserId}, Errors={Errors}",
                        accountId, operation, _currentUser.UserId, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> FilterAccountsAsync(
            IQueryable<Account> query,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var roleResult = await GetCurrentUserRoleAsync();
            if (roleResult.IsFailure)
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Failure(roleResult.Errors);

            var filteringResult = await ApplyRoleBasedFilteringAsync(query, roleResult.Value!, pageNumber, pageSize);
            
            // Add side effects using ResultExtensions
            filteringResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Account filtering completed: Count={Count}, Page={Page}, Size={Size}, Role={Role}", 
                        filteringResult.Value.TotalCount, pageNumber, pageSize, roleResult.Value!.Name);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Account filtering failed: UserId={UserId}, Role={Role}, Errors={Errors}",
                        _currentUser.UserId, roleResult.Value?.Name, string.Join(", ", errors));
                });

            return filteringResult;
        }

        public async Task<Result> CanCreateAccountForUserAsync(string targetUserId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var authorizationResult = await ValidateAccountCreationAuthorizationAsync(targetUserId, scopeResult.Value);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Account creation authorization granted: TargetUserId={TargetUserId}, ActingUserId={ActingUserId}, Scope={Scope}", 
                        targetUserId, _currentUser.UserId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Account creation authorization denied: TargetUserId={TargetUserId}, ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}",
                        targetUserId, _currentUser.UserId, scopeResult.Value, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result<IQueryable<Account>>> FilterAccountsQueryAsync(IQueryable<Account> query)
        {
            var roleResult = await GetCurrentUserRoleAsync();
            if (roleResult.IsFailure)
                return Result<IQueryable<Account>>.Failure(roleResult.Errors);

            var queryFilteringResult = await ApplyRoleBasedQueryFilteringAsync(query, roleResult.Value!);
            
            // Add side effects using ResultExtensions
            queryFilteringResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Account query filtering applied: Role={Role}, UserId={UserId}", 
                        roleResult.Value!.Name, _currentUser.UserId);
                });

            return queryFilteringResult;
        }

        #region Private Helper Methods Using ResultExtensions

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

        private async Task<Result<ApplicationUser>> GetActingUserAsync()
        {
            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            return actingUser.ToResult("Acting user not found.");
        }

        private async Task<Result<Account>> LoadAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult($"Account with ID '{accountId}' not found.");
        }

        private async Task<Result<ApplicationRole>> GetCurrentUserRoleAsync()
        {
            try
            {
                var role = await _currentUser.GetRoleFromStoreAsync();
                return role.ToResult("Failed to retrieve user role.");
            }
            catch (Exception ex)
            {
                return Result<ApplicationRole>.BadRequest($"Failed to get user role: {ex.Message}");
            }
        }

        private async Task<Result> ValidateViewAccountAuthorizationAsync(Account account, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => ValidateSelfViewAuthorization(account),
                AccessScope.BankLevel => ValidateBankLevelViewAuthorization(account),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private Result ValidateSelfViewAuthorization(Account account)
        {
            return _currentUser.UserId.Equals(account.UserId, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.Forbidden("Clients can only view their own accounts.");
        }

        private Result ValidateBankLevelViewAuthorization(Account account)
        {
            return BankGuard.ValidateSameBank(_currentUser.BankId, account.User.BankId);
        }

        private Result ValidateSelfModificationRules(ApplicationUser actingUser, Account account, AccountModificationOperation operation)
        {
            if (!actingUser.Id.Equals(account.UserId, StringComparison.OrdinalIgnoreCase))
                return Result.Success(); // Not self-modification

            return operation switch
            {
                AccountModificationOperation.Edit => Result.Forbidden("Users cannot edit their own accounts."),
                AccountModificationOperation.Delete => Result.Forbidden("Users cannot delete their own accounts."),
                AccountModificationOperation.Freeze or AccountModificationOperation.Unfreeze => 
                    Result.Forbidden("Users cannot freeze or unfreeze their own accounts."),
                AccountModificationOperation.Deposit or AccountModificationOperation.Withdraw => Result.Success(),
                _ => Result.Forbidden("Operation not permitted on own account.")
            };
        }

        private async Task<Result> ValidateAccountModificationAuthorizationAsync(Account account, AccountModificationOperation operation, AccessScope scope, ApplicationUser actingUser)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden("Clients cannot modify other users' accounts."),
                AccessScope.BankLevel => await ValidateBankLevelModificationAuthorizationAsync(account, actingUser),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private async Task<Result> ValidateBankLevelModificationAuthorizationAsync(Account account, ApplicationUser actingUser)
        {
            var roleValidationResult = await ValidateAccountOwnerRoleAsync(account.UserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            var bankAccessResult = BankGuard.ValidateSameBank(actingUser.BankId, account.User.BankId);
            return bankAccessResult;
        }

        private async Task<Result> ValidateAccountOwnerRoleAsync(string accountOwnerId)
        {
            var ownerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(accountOwnerId);
            return RoleHelper.IsClient(ownerRole?.Name)
                ? Result.Success()
                : Result.Forbidden("Only Client accounts can be modified.");
        }

        private async Task<Result> ValidateAccountCreationAuthorizationAsync(string targetUserId, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden("Clients cannot create accounts for other users."),
                AccessScope.BankLevel => await ValidateBankLevelAccountCreationAsync(targetUserId),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private async Task<Result> ValidateBankLevelAccountCreationAsync(string targetUserId)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            var targetUserResult = await LoadTargetUserForCreationAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            var roleValidationResult = await ValidateTargetUserRoleForCreationAsync(targetUserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            var bankAccessResult = BankGuard.ValidateSameBank(actingUserResult.Value!.BankId, targetUserResult.Value!.BankId);
            return bankAccessResult;
        }

        private async Task<Result<ApplicationUser>> LoadTargetUserForCreationAsync(string targetUserId)
        {
            var targetUserSpec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(targetUserSpec);
            return targetUser.ToResult($"Target user with ID '{targetUserId}' not found.");
        }

        private async Task<Result> ValidateTargetUserRoleForCreationAsync(string targetUserId)
        {
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            return RoleHelper.IsClient(targetRole?.Name)
                ? Result.Success()
                : Result.Forbidden("Can only create accounts for Client users.");
        }

        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ApplyRoleBasedFilteringAsync(
            IQueryable<Account> query, ApplicationRole role, int pageNumber, int pageSize)
        {
            try
            {
                if (RoleHelper.IsSuperAdmin(role.Name))
                {
                    return await ExecuteGlobalAccountFilteringAsync(query, pageNumber, pageSize);
                }

                if (RoleHelper.IsClient(role.Name))
                {
                    return await ExecuteSelfAccountFilteringAsync(query, pageNumber, pageSize);
                }

                return await ExecuteBankLevelAccountFilteringAsync(query, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.BadRequest($"Failed to filter accounts: {ex.Message}");
            }
        }

        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteGlobalAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var filteredQuery = query.OrderBy(a => a.Id);
            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteSelfAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var filteredQuery = query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id);
            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteBankLevelAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success((Enumerable.Empty<Account>(), 0));

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            var filteredQuery = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUserResult.Value!.BankId).OrderBy(a => a.Id);
            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        private async Task<Result<IQueryable<Account>>> ApplyRoleBasedQueryFilteringAsync(IQueryable<Account> query, ApplicationRole role)
        {
            try
            {
                if (RoleHelper.IsSuperAdmin(role.Name))
                {
                    return Result<IQueryable<Account>>.Success(query.OrderBy(a => a.Id));
                }

                if (RoleHelper.IsClient(role.Name))
                {
                    return Result<IQueryable<Account>>.Success(query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id));
                }

                var actingUserResult = await GetActingUserAsync();
                if (actingUserResult.IsFailure)
                {
                    return Result<IQueryable<Account>>.Success(Enumerable.Empty<Account>().AsQueryable());
                }

                var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                var filteredQuery = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUserResult.Value!.BankId).OrderBy(a => a.Id);
                return Result<IQueryable<Account>>.Success(filteredQuery);
            }
            catch (Exception ex)
            {
                return Result<IQueryable<Account>>.BadRequest($"Failed to apply query filtering: {ex.Message}");
            }
        }

        #endregion
    }
}
