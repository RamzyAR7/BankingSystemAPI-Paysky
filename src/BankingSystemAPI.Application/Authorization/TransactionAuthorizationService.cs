using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

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

            // SuperAdmin / Global scope should be allowed without validating account existence
            if (scope == AccessScope.Global)
                return;

            var source = await _uow.AccountRepository.FindWithIncludesAsync(
                a => a.Id == sourceAccountId,
                new Expression<Func<Account, object>>[] { a => a.User });

            if (source == null) throw new NotFoundException("Source account not found.");

            var target = await _uow.AccountRepository.FindWithIncludesAsync(
                a => a.Id == targetAccountId,
                new Expression<Func<Account, object>>[] { a => a.User });

            if (target == null) throw new NotFoundException("Target account not found.");

            switch (scope)
            {
                case AccessScope.Global:
                    return; // SuperAdmin full access

                case AccessScope.Self:
                    if (source.UserId != _currentUser.UserId)
                        throw new ForbiddenException("Clients can only transfer from their own accounts.");
                    return;

                case AccessScope.BankLevel:
                    var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                        u => u.Id == _currentUser.UserId,
                        new Expression<Func<ApplicationUser, object>>[] { u => u.Bank });

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
            switch (scope)
            {
                case AccessScope.Global:
                    query = query.OrderBy(t => t.Id);
                    {
                        var res = await _uow.TransactionRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }
                case AccessScope.Self:
                    var userId = _currentUser.UserId;
                    query = query.Where(t => t.AccountTransactions.Any(at => at.Account.UserId == userId)).OrderBy(t => t.Id);
                    {
                        var res = await _uow.TransactionRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }
                case AccessScope.BankLevel:
                    var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                        u => u.Id == _currentUser.UserId,
                        new Expression<Func<ApplicationUser, object>>[] { u => u.Bank });
                    if (actingUser == null)
                        return (Enumerable.Empty<Transaction>(), 0);
                    var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                    query = query.Where(t => t.AccountTransactions.Any(at => clientUserIds.Contains(at.Account.UserId) && at.Account.User.BankId == actingUser.BankId)).OrderBy(t => t.Id);
                    {
                        var res = await _uow.TransactionRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }
                default:
                    return (Enumerable.Empty<Transaction>(), 0);
            }
        }
    }
}
