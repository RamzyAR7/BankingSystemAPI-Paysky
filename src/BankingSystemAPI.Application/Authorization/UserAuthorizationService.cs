using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.AuthorizationServices
{
    public class UserAuthorizationService : IUserAuthorizationService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<UserAuthorizationService> _logger;

        public UserAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver,
            ILogger<UserAuthorizationService> logger)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
            _logger = logger;
        }

        public async Task<Result> CanViewUserAsync(string targetUserId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var authorizationResult = await ValidateViewUserAuthorizationAsync(targetUserId, scopeResult.Value);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] User view authorization granted: TargetUserId={TargetUserId}, Scope={Scope}", 
                        targetUserId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] User view authorization denied: TargetUserId={TargetUserId}, ActingUserId={ActingUserId}, Errors={Errors}",
                        targetUserId, _currentUser.UserId, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result> CanModifyUserAsync(string targetUserId, UserModificationOperation operation)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            var selfModificationResult = ValidateSelfModificationRules(actingUserResult.Value!, targetUserId, operation);
            if (selfModificationResult.IsFailure)
                return selfModificationResult;

            var authorizationResult = await ValidateUserModificationAuthorizationAsync(targetUserId, operation, scopeResult.Value, actingUserResult.Value!);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] User modification authorization granted: TargetUserId={TargetUserId}, Operation={Operation}, Scope={Scope}", 
                        targetUserId, operation, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] User modification authorization denied: TargetUserId={TargetUserId}, Operation={Operation}, ActingUserId={ActingUserId}, Errors={Errors}",
                        targetUserId, operation, _currentUser.UserId, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        public async Task<Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>> FilterUsersAsync(
            IQueryable<ApplicationUser> query,
            int pageNumber = 1,
            int pageSize = 10,
            string? orderBy = null,
            string? orderDirection = null)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>.Failure(scopeResult.Errors);

            var filteringResult = await ApplyScopeFilteringAsync(scopeResult.Value, pageNumber, pageSize, orderBy, orderDirection);
            
            // Add side effects using ResultExtensions
            filteringResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] User filtering completed: Count={Count}, Page={Page}, Size={Size}, Scope={Scope}", 
                        filteringResult.Value.TotalCount, pageNumber, pageSize, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] User filtering failed: ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}",
                        _currentUser.UserId, scopeResult.Value, string.Join(", ", errors));
                });

            return filteringResult;
        }

        public async Task<Result> CanCreateUserAsync()
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return scopeResult;

            var authorizationResult = ValidateUserCreationAuthorization(scopeResult.Value);
            
            // Add side effects using ResultExtensions
            authorizationResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] User creation authorization granted: ActingUserId={ActingUserId}, Scope={Scope}", 
                        _currentUser.UserId, scopeResult.Value);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] User creation authorization denied: ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}",
                        _currentUser.UserId, scopeResult.Value, string.Join(", ", errors));
                });

            return authorizationResult;
        }

        private async Task<Result<AccessScope>> GetScopeAsync()
        {
            try
            {
                var scope = await _scopeResolver.GetScopeAsync();
                return Result<AccessScope>.Success(scope);
            }
            catch (Exception ex)
            {
                return Result<AccessScope>.BadRequest($"Failed to resolve access scope: {ex.Message}");
            }
        }

        private async Task<Result<ApplicationUser>> GetActingUserAsync()
        {
            var actingSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingSpec);
            return actingUser.ToResult("Acting user not found.");
        }

        private async Task<Result> ValidateViewUserAuthorizationAsync(string targetUserId, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => ValidateSelfViewAuthorization(targetUserId),
                AccessScope.BankLevel => await ValidateBankLevelViewAuthorizationAsync(targetUserId),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private Result ValidateSelfViewAuthorization(string targetUserId)
        {
            return _currentUser.UserId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.Forbidden("Clients can only view their own data.");
        }

        private async Task<Result> ValidateBankLevelViewAuthorizationAsync(string targetUserId)
        {
            var targetUserResult = await LoadTargetUserAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            var bankAccessResult = BankGuard.ValidateSameBank(_currentUser.BankId, targetUserResult.Value!.BankId);
            return bankAccessResult;
        }

        private Result ValidateSelfModificationRules(ApplicationUser actingUser, string targetUserId, UserModificationOperation operation)
        {
            if (!actingUser.Id.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
                return Result.Success(); // Not self-modification

            return operation switch
            {
                UserModificationOperation.Delete => Result.Forbidden("Users cannot delete themselves."),
                UserModificationOperation.Edit => Result.Forbidden("Users cannot edit their own profile details (only password)."),
                UserModificationOperation.ChangePassword => Result.Success(),
                _ => Result.Forbidden("Operation not permitted on own account.")
            };
        }

        private async Task<Result> ValidateUserModificationAuthorizationAsync(string targetUserId, UserModificationOperation operation, AccessScope scope, ApplicationUser actingUser)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden("Clients cannot modify other users."),
                AccessScope.BankLevel => await ValidateBankLevelModificationAuthorizationAsync(targetUserId, actingUser),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private async Task<Result> ValidateBankLevelModificationAuthorizationAsync(string targetUserId, ApplicationUser actingUser)
        {
            var targetUserResult = await LoadTargetUserAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            var roleValidationResult = await ValidateTargetUserRoleAsync(targetUserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            var bankAccessResult = BankGuard.ValidateSameBank(actingUser.BankId, targetUserResult.Value!.BankId);
            return bankAccessResult;
        }

        private async Task<Result<ApplicationUser>> LoadTargetUserAsync(string targetUserId)
        {
            var targetSpec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(targetSpec);
            return targetUser.ToResult($"Target user with ID '{targetUserId}' not found.");
        }

        private async Task<Result> ValidateTargetUserRoleAsync(string targetUserId)
        {
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            return RoleHelper.IsClient(targetRole?.Name)
                ? Result.Success()
                : Result.Forbidden("Only Client users can be modified.");
        }

        private Result ValidateUserCreationAuthorization(AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden("Clients cannot create users."),
                AccessScope.BankLevel => Result.Success(),
                _ => Result.Forbidden("Unknown access scope.")
            };
        }

        private async Task<Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>> ApplyScopeFilteringAsync(
            AccessScope scope, int pageNumber, int pageSize, string? orderBy, string? orderDirection)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;
                
                var result = scope switch
                {
                    AccessScope.Global => await ApplyGlobalFilteringAsync(skip, pageSize, orderBy, orderDirection),
                    AccessScope.Self => await ApplySelfFilteringAsync(skip, pageSize, orderBy, orderDirection),
                    AccessScope.BankLevel => await ApplyBankLevelFilteringAsync(skip, pageSize, orderBy, orderDirection),
                    _ => (Users: Enumerable.Empty<ApplicationUser>(), TotalCount: 0)
                };

                await PopulateAccountCurrenciesAsync(result.Users);
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>.BadRequest($"Failed to filter users: {ex.Message}");
            }
        }

        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplyGlobalFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var globalSpec = new PagedSpecification<ApplicationUser>(u => true, skip, pageSize, orderBy, orderDirection, 
                u => u.Accounts, u => u.Bank, u => u.Role);
            var result = await _uow.UserRepository.GetPagedAsync(globalSpec);
            return (result.Items, result.TotalCount);
        }

        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplySelfFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var selfSpec = new PagedSpecification<ApplicationUser>(u => u.Id == _currentUser.UserId, skip, pageSize, 
                orderBy, orderDirection, u => u.Accounts, u => u.Bank, u => u.Role);
            var result = await _uow.UserRepository.GetPagedAsync(selfSpec);
            return (result.Items, result.TotalCount);
        }

        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplyBankLevelFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var bankId = _currentUser.BankId;
            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client");
            Expression<Func<ApplicationUser, bool>> criteria = u => u.BankId == bankId && clientUserIds.Contains(u.Id);
            var bankSpec = new PagedSpecification<ApplicationUser>(criteria, skip, pageSize, orderBy, orderDirection, 
                u => u.Accounts, u => u.Bank, u => u.Role);
            var result = await _uow.UserRepository.GetPagedAsync(bankSpec);
            return (result.Items, result.TotalCount);
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
    }
}
