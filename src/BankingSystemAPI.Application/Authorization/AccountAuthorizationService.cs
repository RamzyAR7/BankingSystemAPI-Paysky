using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
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

        public AccountAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
        }

        public async Task<Result> CanViewAccountAsync(int accountId)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return Result.Success();

            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            if (account == null)
                return Result.NotFound("Account", accountId);

            if (scope == AccessScope.Self)
            {
                if (!_currentUser.UserId.Equals(account.UserId, StringComparison.OrdinalIgnoreCase))
                    return Result.Forbidden("Clients can only view their own accounts.");
                return Result.Success();
            }

            // BankLevel - Use new Result-based guard method
            var bankAccessResult = BankGuard.ValidateSameBank(_currentUser.BankId, account.User.BankId);
            if (bankAccessResult.IsFailure)
                return bankAccessResult;

            return Result.Success();
        }

        public async Task<Result> CanModifyAccountAsync(int accountId, AccountModificationOperation operation)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            if (actingUser == null)
                return Result.Forbidden("Acting user not found.");

            var accountSpec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(accountSpec);
            if (account == null)
                return Result.NotFound("Account", accountId);

            // Self-modification rules
            if (actingUser.Id == account.UserId)
            {
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

            switch (scope)
            {
                case AccessScope.Global:
                    return Result.Success();

                case AccessScope.Self:
                    return Result.Forbidden("Clients cannot modify other users' accounts.");

                case AccessScope.BankLevel:
                    var ownerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(account.UserId);
                    if (!RoleHelper.IsClient(ownerRole?.Name))
                        return Result.Forbidden("Only Client accounts can be modified.");

                    var bankAccessResult = BankGuard.ValidateSameBank(actingUser.BankId, account.User.BankId);
                    if (bankAccessResult.IsFailure)
                        return bankAccessResult;

                    return Result.Success();

                default:
                    return Result.Forbidden("Unknown access scope.");
            }
        }

        public async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> FilterAccountsAsync(
            IQueryable<Account> query,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name))
            {
                query = query.OrderBy(a => a.Id);
                var result = await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
            }

            if (RoleHelper.IsClient(role.Name))
            {
                query = query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id);
                var result = await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
            }

            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            if (actingUser == null)
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success((Enumerable.Empty<Account>(), 0));

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            query = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUser.BankId).OrderBy(a => a.Id);
            var finalResult = await _uow.AccountRepository.GetFilteredAccountsAsync(query, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(finalResult);
        }

        public async Task<Result> CanCreateAccountForUserAsync(string targetUserId)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return Result.Success();

            if (scope == AccessScope.Self)
                return Result.Forbidden("Clients cannot create accounts for other users.");

            // BankLevel
            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            if (actingUser == null)
                return Result.Forbidden("Acting user not found.");

            var targetUserSpec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(targetUserSpec);
            if (targetUser == null)
                return Result.NotFound("Target user", targetUserId);

            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            if (!RoleHelper.IsClient(targetRole?.Name))
                return Result.Forbidden("Can only create accounts for Client users.");

            var bankAccessResult = BankGuard.ValidateSameBank(actingUser.BankId, targetUser.BankId);
            if (bankAccessResult.IsFailure)
                return bankAccessResult;

            return Result.Success();
        }

        public async Task<Result<IQueryable<Account>>> FilterAccountsQueryAsync(IQueryable<Account> query)
        {
            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name))
                return Result<IQueryable<Account>>.Success(query.OrderBy(a => a.Id));

            if (RoleHelper.IsClient(role.Name))
                return Result<IQueryable<Account>>.Success(query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id));

            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            if (actingUser == null)
                return Result<IQueryable<Account>>.Success(Enumerable.Empty<Account>().AsQueryable());

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            var filteredQuery = query.Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUser.BankId).OrderBy(a => a.Id);
            return Result<IQueryable<Account>>.Success(filteredQuery);
        }
    }
}
