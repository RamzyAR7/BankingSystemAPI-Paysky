using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Specifications;
using System.Linq.Expressions;
using System.Linq;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.EntityFrameworkCore;

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

        public async Task CanInitiateTransferAsync(int sourceAccountId, int targetAccountId)
        {
            var scope = await _scopeResolver.GetScopeAsync();
            var srcSpec = new BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification(sourceAccountId);
            var source = await _uow.AccountRepository.FindAsync(srcSpec);

            if (source == null)
                throw new NotFoundException("Source account not found.");

            switch (scope)
            {
                case AccessScope.Global:
                    return;

                case AccessScope.Self:
                    // Acting user must be source owner
                    if (!_currentUser.UserId.Equals(source.UserId, StringComparison.OrdinalIgnoreCase))
                        throw new ForbiddenException("Clients cannot initiate transfers from accounts they don't own.");
                    return;

                case AccessScope.BankLevel:
                    var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
                    var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);

                    if (actingUser == null)
                        throw new ForbiddenException("Acting user not found.");

                    var sourceOwnerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(source.UserId);
                    if (!RoleHelper.IsClient(sourceOwnerRole?.Name))
                        throw new ForbiddenException("Transfers can only be initiated from Client-owned accounts.");

                    BankGuard.EnsureSameBank(actingUser.BankId, source.User.BankId);
                    return;
            }
        }

        public async Task<(IEnumerable<Transaction> Transactions, int TotalCount)> FilterTransactionsAsync(IQueryable<Transaction> query, int pageNumber = 1, int pageSize = 10)
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
                            return (Enumerable.Empty<Transaction>(), 0);

                        var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                        baseQuery = baseQuery.Where(t => t.AccountTransactions.Any(at => clientUserIds.Contains(at.Account.UserId) && at.Account.User.BankId == actingUser.BankId)).OrderByDescending(t => t.Timestamp);
                        break;
                    }

                default:
                    return (Enumerable.Empty<Transaction>(), 0);
            }

            var total = await baseQuery.CountAsync();
            var items = await baseQuery.Skip(skip).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
