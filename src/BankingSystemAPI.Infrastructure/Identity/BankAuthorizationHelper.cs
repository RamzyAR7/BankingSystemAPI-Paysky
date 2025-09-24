using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using BankingSystemAPI.Application.Exceptions;
using System;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using System.Linq;
using System.Collections.Generic;

namespace BankingSystemAPI.Infrastructure.Identity
{
    public class BankAuthorizationHelper : IBankAuthorizationHelper
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;

        public BankAuthorizationHelper(ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager, IUnitOfWork uow)
        {
            _currentUserService = currentUserService;
            _userManager = userManager;
            _uow = uow;
        }

        #region helpers
        // check role
        public async Task<bool> IsSuperAdminAsync()
        {
            var role = await _currentUserService.GetRoleFromStoreAsync();
            return string.Equals(role, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> IsClientAsync()
        {
            var role = await _currentUserService.GetRoleFromStoreAsync();
            return string.Equals(role, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // get obj from login user 
        public async Task<ApplicationUser> GetActingUserAsync()
        {
            var id = _currentUserService.UserId;
            if (string.IsNullOrEmpty(id)) return null;
            return await _userManager.FindByIdAsync(id);
        }
        // get bank id from login user
        public async Task<int?> GetActingUserBankIdAsync()
        {
            var user = await GetActingUserAsync();
            return user?.BankId;
        }
        #endregion

        #region User
        // check if can access bank
        public async Task EnsureCanAccessBankAsync(int targetBankId)
        {
            var isClient = await IsClientAsync();
            if (isClient) throw new ForbiddenException("Clients are not allowed to access other banks.");

            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");
            if (actingUser.BankId != targetBankId) throw new ForbiddenException("Access to requested bank is forbidden.");
        }

        public async Task EnsureCanAccessUserAsync(string targetUserId)
        {
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            // Allow users to access themselves
            if (!string.IsNullOrEmpty(actingUser.Id) && string.Equals(actingUser.Id, targetUserId, StringComparison.OrdinalIgnoreCase))
                return;

            var isClient = await IsClientAsync();
            if (isClient) throw new ForbiddenException("Clients are not allowed to access other users.");

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null) throw new NotFoundException("Target user not found.");

            // Non-client, non-super users may only operate on users who are Clients and within the same bank
            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var isTargetClient = targetRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isTargetClient)
                throw new ForbiddenException("You are only allowed to operate on users with role 'Client'.");

            if (actingUser.BankId != targetUser.BankId)
                throw new ForbiddenException("Access to target user is forbidden due to bank isolation.");
        }

        // New: ensure caller can modify (edit/delete/change password) a user
        public async Task EnsureCanModifyUserAsync(string targetUserId, UserModificationOperation operation)
        {
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            // If acting on self
            if (!string.IsNullOrEmpty(actingUser.Id) && string.Equals(actingUser.Id, targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                if (operation == UserModificationOperation.Delete)
                    throw new ForbiddenException("Users are not allowed to delete themselves.");

                if (operation == UserModificationOperation.Edit)
                    throw new ForbiddenException("Users are only allowed to change their own password. Other self-edits are not permitted.");

                if (operation == UserModificationOperation.ChangePassword)
                {
                    return;
                }
            }

            // Acting on another user
            var isClient = await IsClientAsync();
            if (isClient) throw new ForbiddenException("Clients are not allowed to modify other users.");

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null) throw new NotFoundException("Target user not found.");

            // Non-client, non-super users may only operate on users who are Clients and within the same bank
            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var isTargetClient = targetRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isTargetClient)
                throw new ForbiddenException("You are only allowed to operate on users with role 'Client'.");

            if (actingUser.BankId != targetUser.BankId)
                throw new ForbiddenException("Access to target user is forbidden due to bank isolation.");
        }
        // filter collections without throwing (used for list endpoints)
        public async Task<IEnumerable<ApplicationUser>> FilterUsersAsync(IEnumerable<ApplicationUser> users)
        {
            if (users == null) return Enumerable.Empty<ApplicationUser>();
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return users;

            var isClient = await IsClientAsync();
            if (isClient)
            {
                // clients should see only themselves
                var id = _currentUserService.UserId;
                if (string.IsNullOrEmpty(id)) return Enumerable.Empty<ApplicationUser>();
                return users.Where(u => string.Equals(u.Id, id, StringComparison.OrdinalIgnoreCase));
            }

            // non-client non-super: limit to same bank and only clients
            var actingUser = await GetActingUserAsync();
            if (actingUser == null) return Enumerable.Empty<ApplicationUser>();

            var result = new List<ApplicationUser>();
            foreach (var u in users)
            {
                if (u == null) continue;
                // quick bank check
                if (u.BankId != actingUser.BankId) continue;
                // check role from store
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase)))
                    result.Add(u);
            }

            return result;
        }
        #endregion

        #region Account
        public async Task EnsureCanAccessAccountAsync(int accountId)
        {
            var account = await _uow.AccountRepository.GetByIdAsync(accountId);
            if (account == null) throw new NotFoundException("Account not found.");

            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var isClient = await IsClientAsync();
            if (isClient)
            {
                // clients can only access accounts that belong to their own user id
                var actingUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(actingUserId) || account.UserId != actingUserId)
                    throw new ForbiddenException("Clients can only access their own accounts.");
                return;
            }

            // Non-client non-super users: must be same bank and account owner must be a Client
            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            var accountUser = account.User ?? await _userManager.FindByIdAsync(account.UserId);
            if (accountUser == null) throw new NotFoundException("Account owner not found.");

            var accountUserRoles = await _userManager.GetRolesAsync(accountUser);
            var isAccountUserClient = accountUserRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isAccountUserClient)
                throw new ForbiddenException("You are only allowed to operate on accounts owned by users with role 'Client'.");

            if (actingUser.BankId != accountUser.BankId)
                throw new ForbiddenException("Access to target account is forbidden due to bank isolation.");
        }

        // ensure modification restrictions on accounts
        public async Task EnsureCanModifyAccountAsync(int accountId, AccountModificationOperation operation)
        {
            var account = await _uow.AccountRepository.GetByIdAsync(accountId);
            if (account == null) throw new NotFoundException("Account not found.");

            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            // Owners cannot modify (edit/delete) their own accounts
            if (!string.IsNullOrEmpty(actingUser.Id) && string.Equals(actingUser.Id, account.UserId, StringComparison.OrdinalIgnoreCase))
            {
                if (operation == AccountModificationOperation.Delete || operation == AccountModificationOperation.Edit)
                    throw new ForbiddenException("Users are not allowed to edit or delete their own accounts.");
                return;
            }

            // Acting on another user's account
            var isClient = await IsClientAsync();
            if (isClient) throw new ForbiddenException("Clients are not allowed to modify other users' accounts.");

            var accountUser = account.User ?? await _userManager.FindByIdAsync(account.UserId);
            if (accountUser == null) throw new NotFoundException("Account owner not found.");

            var accountUserRoles = await _userManager.GetRolesAsync(accountUser);
            var isAccountUserClient = accountUserRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isAccountUserClient)
                throw new ForbiddenException("You are only allowed to operate on accounts owned by users with role 'Client'.");

            if (actingUser.BankId != accountUser.BankId)
                throw new ForbiddenException("Access to target account is forbidden due to bank isolation.");
        }

        // allow non-client (not super) users to create accounts for Client users in same bank
        public async Task EnsureCanCreateAccountForUserAsync(string targetUserId)
        {
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return;

            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            var isClient = await IsClientAsync();
            if (isClient) throw new ForbiddenException("Clients cannot create accounts for other users.");

            // non-client (not super) can create accounts for Client users in same bank
            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null) throw new NotFoundException("Target user not found.");

            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var isTargetClient = targetRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isTargetClient) throw new ForbiddenException("Can only create accounts for users with role 'Client'.");

            if (actingUser.BankId != targetUser.BankId) throw new ForbiddenException("Cannot create account for user in different bank.");
        }
        public async Task<IEnumerable<Account>> FilterAccountsAsync(IEnumerable<Account> accounts)
        {
            if (accounts == null) return Enumerable.Empty<Account>();
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return accounts;

            var isClient = await IsClientAsync();
            if (isClient)
            {
                var id = _currentUserService.UserId;
                if (string.IsNullOrEmpty(id)) return Enumerable.Empty<Account>();
                return accounts.Where(a => string.Equals(a.UserId, id, StringComparison.OrdinalIgnoreCase));
            }

            // non-client non-super: only accounts owned by clients in same bank
            var actingUser = await GetActingUserAsync();
            if (actingUser == null) return Enumerable.Empty<Account>();

            var result = new List<Account>();
            foreach (var acc in accounts)
            {
                if (acc == null) continue;
                var owner = acc.User ?? await _userManager.FindByIdAsync(acc.UserId);
                if (owner == null) continue;
                if (owner.BankId != actingUser.BankId) continue;
                var roles = await _userManager.GetRolesAsync(owner);
                var isClientOwner = roles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
                if (isClientOwner) result.Add(acc);
            }
            return result;
        }
        #endregion

        #region Transfer
        public async Task EnsureCanInitiateTransferAsync(int sourceAccountId, int targetAccountId)
        {
            // Validate accounts exist
            var source = await _uow.AccountRepository.GetByIdAsync(sourceAccountId);
            var target = await _uow.AccountRepository.GetByIdAsync(targetAccountId);
            if (source == null) throw new NotFoundException("Source account not found.");
            if (target == null) throw new NotFoundException("Target account not found.");

            var isSuper = await IsSuperAdminAsync();
            var isClient = await IsClientAsync();

            if (isSuper) return; // super can transfer between any accounts

            // Clients can only initiate transfers from their own accounts
            if (isClient)
            {
                var actingUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(actingUserId) || source.UserId != actingUserId)
                    throw new ForbiddenException("Clients can only initiate transfers from their own accounts.");
                return;
            }

            // Non-client, non-super: acting user must belong to same bank as source account and source owner must be Client
            var actingUser = await GetActingUserAsync();
            if (actingUser == null) throw new ForbiddenException("Acting user not found.");

            var sourceUser = source.User ?? await _userManager.FindByIdAsync(source.UserId);
            var targetUser = target.User ?? await _userManager.FindByIdAsync(target.UserId);

            if (sourceUser == null || targetUser == null) throw new NotFoundException("Account owner not found.");

            var sourceUserRoles = await _userManager.GetRolesAsync(sourceUser);
            var isSourceUserClient = sourceUserRoles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!isSourceUserClient)
                throw new ForbiddenException("You are only allowed to initiate transfers from accounts owned by users with role 'Client'.");

            if (actingUser.BankId != sourceUser.BankId)
                throw new ForbiddenException("You are not allowed to initiate transfers from accounts outside your bank.");
        }

        public async Task<IEnumerable<Transaction>> FilterTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            if (transactions == null) return Enumerable.Empty<Transaction>();
            var isSuper = await IsSuperAdminAsync();
            if (isSuper) return transactions;

            var isClient = await IsClientAsync();
            var actingUser = await GetActingUserAsync();
            if (actingUser == null) return Enumerable.Empty<Transaction>();

            var result = new List<Transaction>();
            foreach (var trx in transactions)
            {
                if (trx == null || trx.AccountTransactions == null) continue;

                foreach (var at in trx.AccountTransactions)
                {
                    // try to get account from navigation or repository
                    var acc = at.Account ?? await _uow.AccountRepository.GetByIdAsync(at.AccountId);
                    if (acc == null) continue;

                    var owner = acc.User ?? await _userManager.FindByIdAsync(acc.UserId);
                    if (owner == null) continue;

                    var roles = await _userManager.GetRolesAsync(owner);
                    var isClientOwner = roles.Any(r => string.Equals(r, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (isClient)
                    {
                        // client sees only transactions involving their accounts
                        if (string.Equals(owner.Id, _currentUserService.UserId, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(trx);
                            break;
                        }
                    }
                    else
                    {
                        // non-client non-super: include if owner is client and in same bank
                        if (isClientOwner && owner.BankId == actingUser.BankId)
                        {
                            result.Add(trx);
                            break;
                        }
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
