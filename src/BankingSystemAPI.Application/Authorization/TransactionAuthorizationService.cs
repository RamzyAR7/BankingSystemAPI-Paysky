using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;
using BankingSystemAPI.Application.Specifications;
using System.Linq;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Specifications.UserSpecifications;

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

            var sourceSpec = new AccountByIdSpecification(sourceAccountId);
            var source = await _uow.AccountRepository.FindAsync(sourceSpec);

            if (source == null) throw new NotFoundException("Source account not found.");

            var targetSpec = new AccountByIdSpecification(targetAccountId);
            var target = await _uow.AccountRepository.FindAsync(targetSpec);

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
            switch (scope)
            {
                case AccessScope.Global:
                    {
                        var spec = new PagedSpecification<Transaction>(skip, pageSize, orderByProperty: "Timestamp", orderDirection: "DESC", includes: (t => t.AccountTransactions));
                        var res = await _uow.TransactionRepository.GetPagedAsync(spec);
                        return (res.Items, res.TotalCount);
                    }
                case AccessScope.Self:
                    {
                        var userId = _currentUser.UserId;
                        var spec = new PagedSpecification<Transaction>(t => t.AccountTransactions.Any(at => at.Account.UserId == userId), skip, pageSize, orderByProperty: "Timestamp", orderDirection: "DESC", includes: (t => t.AccountTransactions));
                        var res = await _uow.TransactionRepository.GetPagedAsync(spec);
                        return (res.Items, res.TotalCount);
                    }
                case AccessScope.BankLevel:
                    {
                        var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
                        var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
                        if (actingUser == null)
                            return (Enumerable.Empty<Transaction>(), 0);

                        var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                        Expression<Func<Transaction, bool>> criteria = t => t.AccountTransactions.Any(at => clientUserIds.Contains(at.Account.UserId) && at.Account.User.BankId == actingUser.BankId);
                        var spec = new PagedSpecification<Transaction>(criteria, skip, pageSize, orderByProperty: "Timestamp", orderDirection: "DESC", includes: (t => t.AccountTransactions));
                        var res = await _uow.TransactionRepository.GetPagedAsync(spec);
                        return (res.Items, res.TotalCount);
                    }
                default:
                    return (Enumerable.Empty<Transaction>(), 0);
            }
        }
    }
}
