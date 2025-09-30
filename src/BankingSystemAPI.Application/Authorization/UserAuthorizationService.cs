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

namespace BankingSystemAPI.Application.AuthorizationServices
{
    public class UserAuthorizationService : IUserAuthorizationService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;

        public UserAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
        }

        public async Task CanViewUserAsync(string targetUserId)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return;

            if (scope == AccessScope.Self)
            {
                if (!_currentUser.UserId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
                    throw new ForbiddenException("Clients can only view their own data.");
                return;
            }

            // BankLevel
            var actingBankId = _currentUser.BankId;
            var spec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(spec)
                ?? throw new NotFoundException("Target user not found.");

            BankGuard.EnsureSameBank(actingBankId, targetUser.BankId);
        }

        public async Task CanModifyUserAsync(string targetUserId, UserModificationOperation operation)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            var actingSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingSpec)
                ?? throw new ForbiddenException("Acting user not found.");

            // Prevent self-modification
            if (actingUser.Id.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                if (operation == UserModificationOperation.Delete)
                    throw new ForbiddenException("Users cannot delete themselves.");

                if (operation == UserModificationOperation.Edit)
                    throw new ForbiddenException("Users cannot edit their own profile details (only password).\u00A0");

                if (operation == UserModificationOperation.ChangePassword)
                    return;
            }

            switch (scope)
            {
                case AccessScope.Global:
                    return;

                case AccessScope.Self:
                    throw new ForbiddenException("Clients cannot modify other users.");

                case AccessScope.BankLevel:
                    var targetSpec = new UserByIdSpecification(targetUserId);
                    var targetUser = await _uow.UserRepository.FindAsync(targetSpec)
                        ?? throw new NotFoundException("Target user not found.");

                    var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);

                    if (!RoleHelper.IsClient(targetRole?.Name))
                        throw new ForbiddenException("Only Client users can be modified.");

                    BankGuard.EnsureSameBank(actingUser.BankId, targetUser.BankId);
                    break;
            }
        }

        public async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> FilterUsersAsync(
            IQueryable<ApplicationUser> query,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            var skip = (pageNumber - 1) * pageSize;
            switch (scope)
            {
                case AccessScope.Global:
                    // Paging and filtering in DB via spec
                    var globalSpec = new PagedSpecification<ApplicationUser>(u => true, skip, pageSize, null, null, u => u.Accounts, u => u.Bank, u => u.Role);
                    var globalRes = await _uow.UserRepository.GetPagedAsync(globalSpec);
                    await PopulateAccountCurrenciesAsync(globalRes.Items);
                    return (globalRes.Items, globalRes.TotalCount);

                case AccessScope.Self:
                    var selfSpec = new PagedSpecification<ApplicationUser>(u => u.Id == _currentUser.UserId, skip, pageSize, null, null, u => u.Accounts, u => u.Bank, u => u.Role);
                    var selfRes = await _uow.UserRepository.GetPagedAsync(selfSpec);
                    await PopulateAccountCurrenciesAsync(selfRes.Items);
                    return (selfRes.Items, selfRes.TotalCount);

                case AccessScope.BankLevel:
                    // Filter by BankId and role Client in DB
                    var bankId = _currentUser.BankId;
                    // Subquery for Client users
                    var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client"); // IQueryable<string> of userIds
                    Expression<Func<ApplicationUser, bool>> criteria = u => u.BankId == bankId && clientUserIds.Contains(u.Id);
                    var bankSpec = new PagedSpecification<ApplicationUser>(criteria, skip, pageSize, null, null, u => u.Accounts, u => u.Bank, u => u.Role);
                    var bankRes = await _uow.UserRepository.GetPagedAsync(bankSpec);
                    await PopulateAccountCurrenciesAsync(bankRes.Items);
                    return (bankRes.Items, bankRes.TotalCount);

                default:
                    return (Enumerable.Empty<ApplicationUser>(), 0);
            }
        }

        private async Task PopulateAccountCurrenciesAsync(IEnumerable<ApplicationUser> users)
        {
            if (users == null) return;
            var accountCurrencyIds = users
                .Where(u => u.Accounts != null)
                .SelectMany(u => u.Accounts)
                .Where(a => a != null)
                .Select(a => a.CurrencyId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!accountCurrencyIds.Any()) return;

            var currencyMap = new Dictionary<int, Currency>();
            foreach (var cid in accountCurrencyIds)
            {
                var cur = await _uow.CurrencyRepository.GetByIdAsync(cid);
                if (cur != null)
                    currencyMap[cid] = cur;
            }

            foreach (var user in users)
            {
                if (user.Accounts == null) continue;
                foreach (var acc in user.Accounts)
                {
                    if (acc == null) continue;
                    if (acc.Currency == null && currencyMap.TryGetValue(acc.CurrencyId, out var cur))
                        acc.Currency = cur;
                }
            }
        }

        public async Task CanCreateUserAsync()
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return;

            if (scope == AccessScope.Self)
                throw new ForbiddenException("Clients cannot create users.");
        }
    }
}
