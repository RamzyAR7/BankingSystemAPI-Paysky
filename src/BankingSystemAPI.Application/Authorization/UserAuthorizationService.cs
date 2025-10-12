#region Usings
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
#endregion


namespace BankingSystemAPI.Application.AuthorizationServices
{
    /// <summary>
    /// Comprehensive user authorization service implementing hierarchical access control
    /// with organized validation rules and proper error handling
    /// </summary>
    public class UserAuthorizationService : IUserAuthorizationService
    {
        #region Private Fields

        // Core Dependencies
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<UserAuthorizationService> _logger;

        #endregion

        #region Constructor

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

        #endregion

        #region Public Interface Implementation

        /// <summary>
        /// Validates view access with minimal security impact
        /// Priority: Low-Medium impact operation
        /// </summary>
        public async Task<Result> CanViewUserAsync(string targetUserId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var authorizationResult = await ValidateViewAuthorizationAsync(targetUserId, scopeResult.Value);

            LogAuthorizationResult(
                AuthorizationCheckType.View,
                targetUserId,
                authorizationResult,
                scopeResult.Value);

            return authorizationResult;
        }

        /// <summary>
        /// Validates user creation with medium security impact  
        /// Priority: Medium impact operation
        /// </summary>
        public async Task<Result> CanCreateUserAsync()
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var authorizationResult = ValidateCreationAuthorization(scopeResult.Value);

            LogAuthorizationResult(
                AuthorizationCheckType.Create,
                null,
                authorizationResult,
                scopeResult.Value);

            return authorizationResult;
        }

        /// <summary>
        /// Validates user modification with high security impact
        /// Priority: High impact operation  
        /// </summary>
        public async Task<Result> CanModifyUserAsync(string targetUserId, UserModificationOperation operation)
        {
            // Allow self password changes immediately
            if (operation == UserModificationOperation.ChangePassword &&
                !string.IsNullOrWhiteSpace(_currentUser.UserId) &&
                string.Equals(_currentUser.UserId, targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success();
            }

            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return HandleUserError(actingUserResult);

            // Critical security check - highest priority
            var selfModificationResult = ValidateSelfModificationRules(
                actingUserResult.Value!,
                targetUserId,
                operation);
            if (selfModificationResult.IsFailure)
                return selfModificationResult;

            var authorizationResult = await ValidateModificationAuthorizationAsync(
                targetUserId,
                operation,
                scopeResult.Value,
                actingUserResult.Value!);

            LogAuthorizationResult(
                AuthorizationCheckType.Modify,
                targetUserId,
                authorizationResult,
                scopeResult.Value,
                operation.ToString());

            return authorizationResult;
        }

        /// <summary>
        /// Filters users based on authorization scope
        /// Priority: Low impact operation with potential high data exposure
        /// </summary>
        public async Task<Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>> FilterUsersAsync(
            IQueryable<ApplicationUser> query,
            int pageNumber = 1,
            int pageSize = 10,
            string? orderBy = null,
            string? orderDirection = null)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>.Failure(scopeResult.ErrorItems);

            var filteringResult = await ApplyScopeFilteringAsync(
                scopeResult.Value,
                pageNumber,
                pageSize,
                orderBy,
                orderDirection);

            LogFilteringResult(filteringResult, scopeResult.Value, pageNumber, pageSize);

            return filteringResult;
        }

        #endregion

        #region Core Authorization Logic

        /// <summary>
        /// Validates view authorization with hierarchical scope checking
        /// Order: Self-access → Scope-based validation
        /// </summary>
        private async Task<Result> ValidateViewAuthorizationAsync(string targetUserId, AccessScope scope)
        {
            // Critical Priority: Check self-access first (bypasses all other restrictions)
            if (IsSelfAccess(targetUserId))
            {
                LogSelfAccessGranted(AuthorizationCheckType.View, targetUserId);
                return Result.Success();
            }

            // High Priority: Apply scope-based validation
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked),
                AccessScope.BankLevel => await ValidateBankLevelViewAsync(targetUserId),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        /// <summary>
        /// Validates modification authorization with enhanced security checks
        /// Order: Self-modification rules → Scope validation → Role validation
        /// </summary>
        private async Task<Result> ValidateModificationAuthorizationAsync(
            string targetUserId,
            UserModificationOperation operation,
            AccessScope scope,
            ApplicationUser actingUser)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked),
                AccessScope.BankLevel => await ValidateBankLevelModificationAsync(targetUserId, actingUser),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        /// <summary>
        /// Validates creation authorization based on user scope
        /// Order: Scope hierarchy validation
        /// </summary>
        private Result ValidateCreationAuthorization(AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.ClientsCreateUsersBlocked),
                AccessScope.BankLevel => Result.Success(),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        #endregion

        #region Bank-Level Authorization

        /// <summary>
        /// Bank-level view validation with role and bank isolation checks
        /// Order: User existence → Role validation → Bank isolation
        /// </summary>
        private async Task<Result> ValidateBankLevelViewAsync(string targetUserId)
        {
            // Layer 1: Resource existence validation
            var targetUserResult = await LoadTargetUserAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            // Layer 2: Role-based access validation  
            var roleValidationResult = await ValidateTargetUserRoleForViewAsync(targetUserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            // Layer 3: Bank isolation validation
            return BankGuard.ValidateSameBank(_currentUser.BankId, targetUserResult.Value!.BankId);
        }

        /// <summary>
        /// Bank-level modification validation with comprehensive security checks
        /// Order: User existence → Role validation → Bank isolation
        /// </summary>
        private async Task<Result> ValidateBankLevelModificationAsync(string targetUserId, ApplicationUser actingUser)
        {
            // Layer 1: Resource existence validation
            var targetUserResult = await LoadTargetUserAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            // Layer 2: Role-based access validation
            var roleValidationResult = await ValidateTargetUserRoleForModifyAsync(targetUserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            // Layer 3: Bank isolation validation
            return BankGuard.ValidateSameBank(actingUser.BankId, targetUserResult.Value!.BankId);
        }

        #endregion

        #region Self-Modification Rules

        /// <summary>
        /// Self-modification validation with operation-specific rules
        /// Order: Risk level from highest to lowest impact
        /// </summary>
        private Result ValidateSelfModificationRules(
            ApplicationUser actingUser,
            string targetUserId,
            UserModificationOperation operation)
        {
            if (!IsSelfAccess(actingUser.Id, targetUserId))
                return Result.Success(); // Not self-modification

            return operation switch
            {
                // Critical Risk: Account deletion prevention
                UserModificationOperation.Delete =>
                    Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotDeleteSelf),

                // High Risk: Profile modification restriction  
                UserModificationOperation.Edit =>
                    Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotModifySelf),

                // Low Risk: Password change allowance
                UserModificationOperation.ChangePassword =>
                    Result.Success(),

                // Default: Operation not permitted
                _ => Result.Forbidden("Operation not permitted on own account.")
            };
        }

        #endregion

        #region Role Validation

        /// <summary>
        /// Role validation for view operations with self-access bypass
        /// Order: Self-access check → Role hierarchy validation
        /// </summary>
        private async Task<Result> ValidateTargetUserRoleForViewAsync(string targetUserId)
        {
            // Critical: Always allow self-access regardless of role
            if (IsSelfAccess(targetUserId))
                return Result.Success();

            // High Priority: Enforce role hierarchy for cross-user access
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            return RoleHelper.IsClient(targetRole?.Name)
                ? Result.Success()
                : Result.Forbidden(AuthorizationConstants.ErrorMessages.AdminsViewClientsOnly);
        }

        /// <summary>
        /// Role validation for modification operations with self-access exception for password changes
        /// Order: Self-access check → Role hierarchy validation
        /// </summary>
        private async Task<Result> ValidateTargetUserRoleForModifyAsync(string targetUserId)
        {
            // Critical: Always allow self-access for password changes regardless of role
            if (IsSelfAccess(targetUserId))
                return Result.Success();

            // High Priority: Enforce role hierarchy for cross-user modifications
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            return RoleHelper.IsClient(targetRole?.Name)
                ? Result.Success()
                : Result.Forbidden(AuthorizationConstants.ErrorMessages.OnlyClientsCanBeModified);
        }

        #endregion

        #region Data Filtering

        /// <summary>
        /// Apply scope-based filtering with proper data isolation
        /// Order: Scope complexity from simple to complex
        /// </summary>
        private async Task<Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>> ApplyScopeFilteringAsync(
            AccessScope scope,
            int pageNumber,
            int pageSize,
            string? orderBy,
            string? orderDirection)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;

                var result = scope switch
                {
                    // Simple: Single user access
                    AccessScope.Self => await ApplySelfFilteringAsync(skip, pageSize, orderBy, orderDirection),

                    // Medium: Bank-scoped access with role filtering
                    AccessScope.BankLevel => await ApplyBankLevelFilteringAsync(skip, pageSize, orderBy, orderDirection),

                    // Complex: Global access (no filtering)
                    AccessScope.Global => await ApplyGlobalFilteringAsync(skip, pageSize, orderBy, orderDirection),

                    // Default: No access
                    _ => (Users: Enumerable.Empty<ApplicationUser>(), TotalCount: 0)
                };

                await PopulateAccountCurrenciesAsync(result.Users);
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>.Success(result);
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to filter users", ex);
                return Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>
                    .BadRequest($"Failed to filter users: {ex.Message}");
            }
        }

        /// <summary>
        /// Global filtering - least restrictive (SuperAdmin access)
        /// </summary>
        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplyGlobalFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var globalSpec = new PagedSpecification<ApplicationUser>(
                u => true,
                skip,
                pageSize,
                orderBy,
                orderDirection,
                u => u.Accounts,
                u => u.Bank,
                u => u.Role);

            var result = await _uow.UserRepository.GetPagedAsync(globalSpec);
            return (result.Items, result.TotalCount);
        }

        /// <summary>
        /// Self filtering - most restrictive (Client access)
        /// </summary>
        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplySelfFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var selfSpec = new PagedSpecification<ApplicationUser>(
                u => u.Id == _currentUser.UserId,
                skip,
                pageSize,
                orderBy,
                orderDirection,
                u => u.Accounts,
                u => u.Bank,
                u => u.Role);

            var result = await _uow.UserRepository.GetPagedAsync(selfSpec);
            return (result.Items, result.TotalCount);
        }

        /// <summary>
        /// Bank-level filtering - moderately restrictive (Admin access to Clients only)
        /// </summary>
        private async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> ApplyBankLevelFilteringAsync(
            int skip, int pageSize, string? orderBy, string? orderDirection)
        {
            var bankId = _currentUser.BankId;
            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery("Client");

            Expression<Func<ApplicationUser, bool>> criteria = u =>
                u.BankId == bankId && clientUserIds.Contains(u.Id);

            var bankSpec = new PagedSpecification<ApplicationUser>(
                criteria,
                skip,
                pageSize,
                orderBy,
                orderDirection,
                u => u.Accounts,
                u => u.Bank,
                u => u.Role);

            var result = await _uow.UserRepository.GetPagedAsync(bankSpec);
            return (result.Items, result.TotalCount);
        }

        #endregion

        #region Helper Methods

        // Core Data Access Helpers
        private async Task<Result<AccessScope>> GetScopeAsync()
        {
            try
            {
                var scope = await _scopeResolver.GetScopeAsync();
                return Result<AccessScope>.Success(scope);
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to resolve access scope", ex);
                return Result<AccessScope>.BadRequest($"Failed to resolve access scope: {ex.Message}");
            }
        }

        private async Task<Result<ApplicationUser>> GetActingUserAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                return Result<ApplicationUser>.Failure(ErrorType.Unauthorized, "Acting user is not available in the current context.");

            var actingSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingSpec);
            return actingUser.ToResult("Acting user not found.");
        }

        private async Task<Result<ApplicationUser>> LoadTargetUserAsync(string targetUserId)
        {
            var targetSpec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(targetSpec);
            return targetUser.ToResult($"Target user with ID '{targetUserId}' not found.");
        }

        // Validation Helpers
        private bool IsSelfAccess(string targetUserId) =>
            !string.IsNullOrEmpty(_currentUser.UserId) && string.Equals(_currentUser.UserId, targetUserId, StringComparison.OrdinalIgnoreCase);

        private bool IsSelfAccess(string actingUserId, string targetUserId) =>
            actingUserId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase);

        // Error Handling Helpers
        private Result HandleScopeError(Result<AccessScope> scopeResult) =>
            Result.BadRequest(scopeResult.Errors.FirstOrDefault() ?? AuthorizationConstants.ErrorMessages.SystemError);

        private Result HandleUserError(Result<ApplicationUser> userResult) =>
            Result.BadRequest(userResult.Errors.FirstOrDefault() ?? AuthorizationConstants.ErrorMessages.ResourceNotFound);

        // Data Enhancement Helpers
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

        #endregion

        #region Logging Methods

        private void LogAuthorizationResult(
            AuthorizationCheckType checkType,
            string? targetUserId,
            Result authResult,
            AccessScope scope,
            string? additionalInfo = null)
        {
            if (authResult.IsSuccess)
            {
                _logger.LogDebug(ApiResponseMessages.Logging.AuthorizationGrantedGeneric,
                    AuthorizationConstants.LoggingCategories.ACCESS_GRANTED,
                    checkType,
                    targetUserId,
                    scope,
                    additionalInfo ?? "N/A");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.AuthorizationDeniedGeneric,
                    AuthorizationConstants.LoggingCategories.ACCESS_DENIED,
                    checkType,
                    targetUserId,
                    _currentUser.UserId,
                    scope,
                    string.Join(", ", authResult.Errors),
                    additionalInfo ?? "N/A");
            }
        }

        private void LogFilteringResult<T>(
            Result<T> filterResult,
            AccessScope scope,
            int pageNumber,
            int pageSize)
        {
            if (filterResult.IsSuccess)
            {
                _logger.LogDebug(ApiResponseMessages.Logging.UserFilteringCompleted,
                    AuthorizationConstants.LoggingCategories.AUTHORIZATION_CHECK,
                    scope,
                    pageNumber,
                    pageSize,
                    _currentUser.UserId);
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.UserFilteringFailed,
                    AuthorizationConstants.LoggingCategories.SYSTEM_ERROR,
                    _currentUser.UserId,
                    scope,
                    string.Join(", ", filterResult.Errors));
            }
        }

        private void LogSelfAccessGranted(AuthorizationCheckType checkType, string targetUserId)
        {
            _logger.LogDebug(ApiResponseMessages.Logging.SelfAccessGranted,
                AuthorizationConstants.LoggingCategories.ACCESS_GRANTED,
                checkType,
                targetUserId);
        }

        private void LogSystemError(string message, Exception? ex = null)
        {
            _logger.LogError(ex,
                "{LogCategory} {Message}: ActingUserId={ActingUserId}",
                AuthorizationConstants.LoggingCategories.SYSTEM_ERROR,
                message,
                _currentUser.UserId);
        }

        #endregion
    }
}
