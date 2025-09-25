using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BankingSystemAPI.Application.AuthorizationServices
{
    public class AccountAuthorizationService : IAccountAuthorizationService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;

        public AccountAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
        }

        public async Task CanViewAccountAsync(int accountId)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return;

            var account = await _uow.AccountRepository.FindWithIncludesAsync(
                a => a.Id == accountId,
                new Expression<Func<Account, object>>[] { a => a.User })
                ?? throw new NotFoundException("Account not found.");

            if (scope == AccessScope.Self)
            {
                if (!_currentUser.UserId.Equals(account.UserId, StringComparison.OrdinalIgnoreCase))
                    throw new ForbiddenException("Clients can only view their own accounts.");
                return;
            }

            // BankLevel
            BankGuard.EnsureSameBank(_currentUser.BankId, account.User.BankId);
        }

        public async Task CanModifyAccountAsync(int accountId, AccountModificationOperation operation)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
                ?? throw new ForbiddenException("Acting user not found.");

            var account = await _uow.AccountRepository.FindWithIncludesAsync(
                a => a.Id == accountId,
                new Expression<Func<Account, object>>[] { a => a.User })
                ?? throw new NotFoundException("Account not found.");

            // Prevent self modification (even admins)
            if (actingUser.Id == account.UserId)
            {
                if (operation == AccountModificationOperation.Edit || operation == AccountModificationOperation.Delete)
                    throw new ForbiddenException("Users cannot edit or delete their own accounts.");
                return;
            }

            switch (scope)
            {
                case AccessScope.Global:
                    return;

                case AccessScope.Self:
                    throw new ForbiddenException("Clients cannot modify other users' accounts.");

                case AccessScope.BankLevel:
                    var ownerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(account.UserId);
                    if (!RoleHelper.IsClient(ownerRole?.Name))
                        throw new ForbiddenException("Only Client accounts can be modified.");

                    BankGuard.EnsureSameBank(actingUser.BankId, account.User.BankId);
                    break;
            }
        }

        public async Task<IEnumerable<Account>> FilterAccountsAsync(IEnumerable<Account> accounts)
        {
            if (accounts == null) return Enumerable.Empty<Account>();

            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name)) return accounts;

            if (RoleHelper.IsClient(role.Name))
                return accounts.Where(a => a.UserId == _currentUser.UserId);

            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Accounts, u => u.Bank }
            );
            if (actingUser == null) return Enumerable.Empty<Account>();

            var acctList = accounts.ToList();

            // 1- Collect userIds
            var allUserIds = acctList
                .Select(a => a.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // 2️- Fetch users + banks
            var fetchedUsers = await _uow.UserRepository.GetUsersByIdsAsync(
                allUserIds,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank }
            );
            var usersById = fetchedUsers.ToDictionary(u => u.Id, u => u);

            // 3️- Fetch all roles in batch
            var rolesByUser = await _uow.RoleRepository.GetRolesByUserIdsAsync(allUserIds);

            // 4️- Filter
            var result = new List<Account>();
            foreach (var a in acctList)
            {
                if (!usersById.TryGetValue(a.UserId, out var user))
                    continue;

                if (user.BankId != actingUser.BankId)
                    continue;

                if (rolesByUser.TryGetValue(user.Id, out var roleName) && RoleHelper.IsClient(roleName))
                    result.Add(a);
            }

            return result;
        }


        public async Task CanCreateAccountForUserAsync(string targetUserId)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return;

            if (scope == AccessScope.Self)
                throw new ForbiddenException("Clients cannot create accounts for other users.");

            // BankLevel
            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
                ?? throw new ForbiddenException("Acting user not found.");

            var targetUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == targetUserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
                ?? throw new NotFoundException("Target user not found.");

            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            if (!RoleHelper.IsClient(targetRole?.Name))
                throw new ForbiddenException("Can only create accounts for Client users.");

            BankGuard.EnsureSameBank(actingUser.BankId, targetUser.BankId);
        }
    }
}
