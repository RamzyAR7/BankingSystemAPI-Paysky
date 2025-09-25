using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<ApplicationUser>> FilterUsersAsync(IEnumerable<ApplicationUser> users)
        {
            if (users == null) return Enumerable.Empty<ApplicationUser>();

            var scope = await _scopeResolver.GetScopeAsync();

            switch (scope)
            {
                case AccessScope.Global:
                    return users;

                case AccessScope.Self:
                    return users.Where(u => u.Id == _currentUser.UserId);

                case AccessScope.BankLevel:
                    var actingUser = await _uow.UserRepository.FindWithIncludesAsync(
                        u => u.Id == _currentUser.UserId,
                        new Expression<Func<ApplicationUser, object>>[] { u => u.Bank });

                    if (actingUser == null) return Enumerable.Empty<ApplicationUser>();

                    var userList = users.ToList();
                    var missingBankUserIds = userList
                        .Where(u => u.Bank == null)
                        .Select(u => u.Id)
                        .ToList();

                    // batch fetch users with Bank for those missing
                    var fetchedUsers = await _uow.UserRepository.FindAllWithIncludesAsync(
                        u => missingBankUserIds.Contains(u.Id),
                        new Expression<Func<ApplicationUser, object>>[] { u => u.Bank });

                    var resolvedUsers = userList
                        .Select(u => u.Bank == null
                            ? fetchedUsers.FirstOrDefault(x => x.Id == u.Id) ?? u
                            : u)
                        .Where(u => u != null)
                        .ToList();

                    // batch load roles
                    var userIds = resolvedUsers.Select(u => u.Id).ToList();
                    var rolesByUser = await _uow.RoleRepository.GetRolesByUserIdsAsync(userIds);

                    return resolvedUsers
                        .Where(u =>
                            u.BankId == actingUser.BankId &&
                            rolesByUser.TryGetValue(u.Id, out var roleName) &&
                            RoleHelper.IsClient(roleName))
                        .ToList();

                default:
                    return Enumerable.Empty<ApplicationUser>();
            }
        }


        public async Task CanCreateUserAsync()
        {
            var scope = await _scopeResolver.GetScopeAsync();

            if (scope == AccessScope.Global)
                return;

            if (scope == AccessScope.Self)
                throw new ForbiddenException("Clients cannot create users.");

            // BankLevel roles can create users (Clients)
        }
    }
}
