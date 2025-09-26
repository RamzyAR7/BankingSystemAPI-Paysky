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

            // Self-modification rules
            if (actingUser.Id == account.UserId)
            {
                switch (operation)
                {
                    case AccountModificationOperation.Edit:
                        throw new ForbiddenException("Users cannot edit their own accounts.");
                    case AccountModificationOperation.Delete:
                        throw new ForbiddenException("Users cannot delete their own accounts.");
                    case AccountModificationOperation.Freeze:
                    case AccountModificationOperation.Unfreeze:
                        throw new ForbiddenException("Users cannot freeze or unfreeze their own accounts.");
                    case AccountModificationOperation.Deposit:
                    case AccountModificationOperation.Withdraw:
                        // Allow deposit/withdraw on own account
                        return;
                    default:
                        throw new ForbiddenException("Operation not permitted on own account.");
                }
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

        public async Task<(IEnumerable<Account> Accounts, int TotalCount)> FilterAccountsAsync(
            IQueryable<Account> query,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name))
            {
                query = query.OrderBy(a => a.Id);
                return await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
            }

            if (RoleHelper.IsClient(role.Name))
            {
                query = query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id);
                return await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
            }

            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank }
            );
            if (actingUser == null)
                return (Enumerable.Empty<Account>(), 0);

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client"); // IQueryable<string> of userIds
            query = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUser.BankId).OrderBy(a => a.Id);
            return await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
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

        public async Task<IQueryable<Account>> FilterAccountsQueryAsync(IQueryable<Account> query)
        {
            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name))
                return query.OrderBy(a => a.Id);

            if (RoleHelper.IsClient(role.Name))
                return query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id);

            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank }
            );
            if (actingUser == null)
                return Enumerable.Empty<Account>().AsQueryable();

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client"); // IQueryable<string> of userIds
            query = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUser.BankId).OrderBy(a => a.Id);
            return query;
        }
    }
}
