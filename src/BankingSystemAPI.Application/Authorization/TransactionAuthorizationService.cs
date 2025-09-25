using BankingSystemAPI.Application.Authorization.Helpers;
using System.Linq.Expressions;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;

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

            var source = await _uow.AccountRepository.FindAsync(
                a => a.Id == sourceAccountId,
                new[] { "User" })
                ?? throw new NotFoundException("Source account not found.");

            var target = await _uow.AccountRepository.FindAsync(
                a => a.Id == targetAccountId,
                new[] { "User" })
                ?? throw new NotFoundException("Target account not found.");

            switch (scope)
            {
                case AccessScope.Global:
                    return; // SuperAdmin full access

                case AccessScope.Self:
                    if (source.UserId != _currentUser.UserId)
                        throw new ForbiddenException("Clients can only transfer from their own accounts.");
                    return;

                case AccessScope.BankLevel:
                    var actingUser = await _uow.UserRepository.FindAsync(
                        u => u.Id == _currentUser.UserId,
                        new[] { "Bank" })
                        ?? throw new ForbiddenException("Acting user not found.");

                    var sourceOwnerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(source.UserId);
                    if (!RoleHelper.IsClient(sourceOwnerRole?.Name))
                        throw new ForbiddenException("Transfers can only be initiated from Client-owned accounts.");

                    BankGuard.EnsureSameBank(actingUser.BankId, source.User.BankId);
                    return;
            }
        }

        public async Task<IEnumerable<Transaction>> FilterTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            if (transactions == null) return Enumerable.Empty<Transaction>();

            var scope = await _scopeResolver.GetScopeAsync();
            if (scope == AccessScope.Global) return transactions;

            var trxList = transactions.ToList();

            // collect account ids
            var accountIds = trxList
                .Where(t => t?.AccountTransactions != null)
                .SelectMany(t => t.AccountTransactions)
                .Select(at => at.AccountId)
                .Distinct()
                .ToList();

            // batch load accounts + user
            var accounts = await _uow.AccountRepository.FindAllWithIncludesAsync(
                a => accountIds.Contains(a.Id),
                new Expression<Func<Account, object>>[] { a => a.User });

            var accountsById = accounts.ToDictionary(a => a.Id, a => a);

            // batch load roles
            var userIds = accountsById.Values
                .Select(a => a.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var rolesByUser = await _uow.RoleRepository.GetRolesByUserIdsAsync(userIds);

            if (scope == AccessScope.Self)
            {
                return trxList.Where(trx =>
                    trx?.AccountTransactions?.Any(at =>
                        accountsById.TryGetValue(at.AccountId, out var acc) &&
                        acc.UserId == _currentUser.UserId) ?? false);
            }

            if (scope == AccessScope.BankLevel)
            {
                var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                    u => u.Id == _currentUser.UserId,
                    new Expression<Func<ApplicationUser, object>>[] { u => u.Bank });

                if (actingUser == null) return Enumerable.Empty<Transaction>();

                var result = new List<Transaction>();

                foreach (var trx in trxList)
                {
                    if (trx?.AccountTransactions == null) continue;

                    foreach (var at in trx.AccountTransactions)
                    {
                        if (!accountsById.TryGetValue(at.AccountId, out var acc)) continue;
                        if (acc?.User == null) continue;

                        if (rolesByUser.TryGetValue(acc.UserId, out var ownerRoleName))
                        {
                            if (RoleHelper.IsClient(ownerRoleName) && acc.User.BankId == actingUser.BankId)
                            {
                                result.Add(trx);
                                break;
                            }
                        }
                    }
                }

                return result;
            }

            return Enumerable.Empty<Transaction>();
        }
    }
}
