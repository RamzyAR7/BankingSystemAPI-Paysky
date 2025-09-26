using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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
            var targetUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == targetUserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
                ?? throw new NotFoundException("Target user not found.");

            BankGuard.EnsureSameBank(actingBankId, targetUser.BankId);
        }

        public async Task CanModifyUserAsync(string targetUserId, UserModificationOperation operation)
        {
            var scope = await _scopeResolver.GetScopeAsync();

            var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                u => u.Id == _currentUser.UserId,
                new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
                ?? throw new ForbiddenException("Acting user not found.");

            // Prevent self-modification
            if (actingUser.Id.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                if (operation == UserModificationOperation.Delete)
                    throw new ForbiddenException("Users cannot delete themselves.");

                if (operation == UserModificationOperation.Edit)
                    throw new ForbiddenException("Users cannot edit their own profile details (only password).");

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
                    var targetUser = await _uow.UserRepository.FindWithIncludesAsync(
                        u => u.Id == targetUserId,
                        new Expression<Func<ApplicationUser, object>>[] { u => u.Bank })
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

            switch (scope)
            {
                case AccessScope.Global:
                    // Paging and filtering in DB
                    query = query.OrderBy(u => u.Id);
                    {
                        var res = await _uow.UserRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }

                case AccessScope.Self:
                    query = query.Where(u => u.Id == _currentUser.UserId).OrderBy(u => u.Id);
                    {
                        var res = await _uow.UserRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }

                case AccessScope.BankLevel:
                    // Filter by BankId and role Client in DB
                    var bankId = _currentUser.BankId;
                    // Subquery for Client users
                    var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client"); // IQueryable<string> of userIds
                    query = query.Where(u => u.BankId == bankId && clientUserIds.Contains(u.Id)).OrderBy(u => u.Id);
                    {
                        var res = await _uow.UserRepository.GetPagedAsync(query, pageNumber, pageSize);
                        return (res.Items, res.TotalCount);
                    }

                default:
                    return (Enumerable.Empty<ApplicationUser>(), 0);
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
