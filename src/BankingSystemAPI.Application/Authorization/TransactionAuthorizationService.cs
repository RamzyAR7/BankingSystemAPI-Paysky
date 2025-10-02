using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.EntityFrameworkCore;
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

        public TransactionAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
        }

        public async Task<Result> CanInitiateTransferAsync(int sourceAccountId, int targetAccountId)
        {
            var scope = await _scopeResolver.GetScopeAsync();
            var srcSpec = new BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification(sourceAccountId);
            var source = await _uow.AccountRepository.FindAsync(srcSpec);

            if (source == null)
                return Result.NotFound("Source account", sourceAccountId);

            switch (scope)
            {
                case AccessScope.Global:
                    return Result.Success();

                case AccessScope.Self:
                    // Acting user must be source owner
                    if (!_currentUser.UserId.Equals(source.UserId, StringComparison.OrdinalIgnoreCase))
                        return Result.Forbidden("Clients cannot initiate transfers from accounts they don't own.");
                    return Result.Success();

                case AccessScope.BankLevel:
                    var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
                    var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);

                    if (actingUser == null)
                        return Result.Forbidden("Acting user not found.");

                    var sourceOwnerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(source.UserId);
                    if (!RoleHelper.IsClient(sourceOwnerRole?.Name))
                        return Result.Forbidden("Transfers can only be initiated from Client-owned accounts.");

                    var bankAccessResult = BankGuard.ValidateSameBank(actingUser.BankId, source.User.BankId);
                    if (bankAccessResult.IsFailure)
                        return bankAccessResult;

                    return Result.Success();

                default:
                    return Result.Forbidden("Unknown access scope.");
            }
        }

        public async Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> FilterTransactionsAsync(IQueryable<Transaction> query, int pageNumber = 1, int pageSize = 10)
        {
            var scope = await _scopeResolver.GetScopeAsync();
            var skip = (pageNumber - 1) * pageSize;

            // Start with the provided query so caller-provided filters (e.g. by account id) are preserved
            var baseQuery = query;

            switch (scope)
            {
                case AccessScope.Global:
                    baseQuery = baseQuery.OrderByDescending(t => t.Timestamp);
                    break;

                case AccessScope.Self:
                    {
                        var userId = _currentUser.UserId;
                        baseQuery = baseQuery.Where(t => t.AccountTransactions.Any(at => at.Account.UserId == userId)).OrderByDescending(t => t.Timestamp);
                        break;
                    }

                case AccessScope.BankLevel:
                    {
                        var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
                        var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
                        if (actingUser == null)
                            return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((Enumerable.Empty<Transaction>(), 0));

                        var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                        baseQuery = baseQuery.Where(t => t.AccountTransactions.Any(at => clientUserIds.Contains(at.Account.UserId) && at.Account.User.BankId == actingUser.BankId)).OrderByDescending(t => t.Timestamp);
                        break;
                    }

                default:
                    return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((Enumerable.Empty<Transaction>(), 0));
            }

            var total = await baseQuery.CountAsync();
            var items = await baseQuery.Skip(skip).Take(pageSize).ToListAsync();
            return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((items, total));
        }
    }
}
