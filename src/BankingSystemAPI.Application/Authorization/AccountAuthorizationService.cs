#region Usings
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Specifications.AccountSpecification;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.AuthorizationServices
{
    /// <summary>
    /// Comprehensive account authorization service implementing hierarchical access control
    /// with organized validation rules and proper error handling for financial account operations
    /// </summary>
    public class AccountAuthorizationService : IAccountAuthorizationService
    {

        #region Private Fields - Organized by Responsibility

        // Core Dependencies
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<AccountAuthorizationService> _logger;

        #endregion

        #region Constructor

        public AccountAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver,
            ILogger<AccountAuthorizationService> logger)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
            _logger = logger;
        }

        #endregion

        #region Public Interface Implementation - Ordered by Operation Impact

        /// <summary>
        /// Validates account view access with minimal security impact
        /// Priority: Low-Medium impact operation for financial data viewing
        /// </summary>
        public async Task<Result> CanViewAccountAsync(int accountId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var accountResult = await LoadAccountAsync(accountId);
            if (accountResult.IsFailure)
                return HandleAccountError(accountResult);

            var authorizationResult = await ValidateViewAuthorizationAsync(accountResult.Value!, scopeResult.Value);

            LogAuthorizationResult(
                AuthorizationCheckType.View,
                accountId.ToString(),
                authorizationResult,
                scopeResult.Value);

            return authorizationResult;
        }

        /// <summary>
        /// Validates account creation with medium-high security impact
        /// Priority: Medium-High impact operation for financial account creation
        /// </summary>
        public async Task<Result> CanCreateAccountForUserAsync(string targetUserId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var authorizationResult = await ValidateCreationAuthorizationAsync(targetUserId, scopeResult.Value);

            LogAuthorizationResult(
                AuthorizationCheckType.Create,
                targetUserId,
                authorizationResult,
                scopeResult.Value);

            return authorizationResult;
        }

        /// <summary>
        /// Validates account modification with high security impact
        /// Priority: High impact operation for financial account modifications
        /// </summary>
        public async Task<Result> CanModifyAccountAsync(int accountId, AccountModificationOperation operation)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return HandleUserError(actingUserResult);

            var accountResult = await LoadAccountAsync(accountId);
            if (accountResult.IsFailure)
                return HandleAccountError(accountResult);

            // Critical security check - highest priority for financial operations
            var selfModificationResult = ValidateSelfModificationRules(
                actingUserResult.Value!,
                accountResult.Value!,
                operation);
            if (selfModificationResult.IsFailure)
                return selfModificationResult;

            var authorizationResult = await ValidateModificationAuthorizationAsync(
                accountResult.Value!,
                operation,
                scopeResult.Value,
                actingUserResult.Value!);

            LogAuthorizationResult(
                AuthorizationCheckType.Modify,
                accountId.ToString(),
                authorizationResult,
                scopeResult.Value,
                operation.ToString());

            return authorizationResult;
        }

        /// <summary>
        /// Filters accounts based on authorization scope
        /// Priority: Low impact operation with potential high financial data exposure
        /// </summary>
        public async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> FilterAccountsAsync(
            IQueryable<Account> query,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var roleResult = await GetCurrentUserRoleAsync();

            if (roleResult.IsFailure)
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Failure(roleResult.ErrorItems);
            var filteringResult = await ApplyRoleBasedFilteringAsync(
                query,
                roleResult.Value!,
                pageNumber,
                pageSize);

            LogFilteringResult(filteringResult, roleResult.Value?.Name ?? "Unknown", pageNumber, pageSize);

            return filteringResult;
        }

        /// <summary>
        /// Provides filtered query for accounts based on user permissions
        /// Priority: Low impact operation with controlled data access
        /// </summary>
        public async Task<Result<IQueryable<Account>>> FilterAccountsQueryAsync(IQueryable<Account> query)
        {
            var roleResult = await GetCurrentUserRoleAsync();

            if (roleResult.IsFailure)
                return Result<IQueryable<Account>>.Failure(roleResult.ErrorItems);
            var queryFilteringResult = await ApplyRoleBasedQueryFilteringAsync(query, roleResult.Value!);

            LogQueryFilteringResult(queryFilteringResult, roleResult.Value?.Name ?? "Unknown");

            return queryFilteringResult;
        }

        #endregion

        #region Core Authorization Logic - Organized by Validation Type

        /// <summary>
        /// Validates view authorization with hierarchical scope checking for account access
        /// Order: Self-access → Scope-based validation → Financial data protection
        /// </summary>
        private async Task<Result> ValidateViewAuthorizationAsync(Account account, AccessScope scope)
        {
            // Critical Priority: Check self-access first (bypasses other restrictions for own accounts)
            if (IsSelfAccess(account.UserId))
            {
                LogSelfAccessGranted(AuthorizationCheckType.View, account.Id.ToString());
                return Result.Success();
            }

            // High Priority: Apply scope-based validation for financial data
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.AccountOwnershipRequired),
                AccessScope.BankLevel => await ValidateBankLevelViewAsync(account),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        /// <summary>
        /// Validates modification authorization with enhanced security checks for financial operations
        /// Order: Self-modification rules → Scope validation → Role validation → Financial protection
        /// </summary>
        private async Task<Result> ValidateModificationAuthorizationAsync(
            Account account,
            AccountModificationOperation operation,
            AccessScope scope,
            ApplicationUser actingUser)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked),
                AccessScope.BankLevel => await ValidateBankLevelModificationAsync(account, actingUser),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        /// <summary>
        /// Validates creation authorization based on user scope for new financial accounts
        /// Order: Scope hierarchy validation → Target user validation → Bank isolation
        /// </summary>
        private async Task<Result> ValidateCreationAuthorizationAsync(string targetUserId, AccessScope scope)
        {
            return scope switch
            {
                AccessScope.Global => Result.Success(),
                AccessScope.Self => Result.Forbidden(AuthorizationConstants.ErrorMessages.ClientsCreateUsersBlocked),
                AccessScope.BankLevel => await ValidateBankLevelCreationAsync(targetUserId),
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        #endregion

        #region Bank-Level Authorization - Organized by Security Layer

        /// <summary>
        /// Bank-level view validation with account ownership and bank isolation checks
        /// Order: Account owner role validation → Bank isolation → Financial data protection
        /// </summary>
        private async Task<Result> ValidateBankLevelViewAsync(Account account)
        {
            // Layer 1: Role-based access validation - Admins can only view Client accounts
            var roleValidationResult = await ValidateAccountOwnerRoleForViewAsync(account.UserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            // Layer 2: Bank isolation validation for financial data
            return BankGuard.ValidateSameBank(_currentUser.BankId, account.User?.BankId);
        }

        /// <summary>
        /// Bank-level modification validation with comprehensive security checks for financial operations
        /// Order: Account owner role → Bank isolation → Financial operation validation
        /// </summary>
        private async Task<Result> ValidateBankLevelModificationAsync(Account account, ApplicationUser actingUser)
        {
            // Layer 1: Role-based access validation for financial operations
            var roleValidationResult = await ValidateAccountOwnerRoleAsync(account.UserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            // Layer 2: Bank isolation validation for financial security
            return BankGuard.ValidateSameBank(actingUser.BankId, account.User?.BankId);
        }

        /// <summary>
        /// Bank-level creation validation for new financial accounts
        /// Order: Target user existence → Role validation → Bank isolation
        /// </summary>
        private async Task<Result> ValidateBankLevelCreationAsync(string targetUserId)
        {
            // Layer 1: Acting user validation
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            // Layer 2: Target user existence validation
            var targetUserResult = await LoadTargetUserForCreationAsync(targetUserId);
            if (targetUserResult.IsFailure)
                return targetUserResult;

            // Layer 3: Role-based access validation
            var roleValidationResult = await ValidateTargetUserRoleForCreationAsync(targetUserId);
            if (roleValidationResult.IsFailure)
                return Result.Forbidden(AuthorizationConstants.ErrorMessages.AccountCreationAllowedForClients);

            // Layer 4: Bank isolation validation
            return BankGuard.ValidateSameBank(actingUserResult.Value!.BankId, targetUserResult.Value!.BankId);
        }

        #endregion

        #region Self-Modification Rules - Organized by Financial Operation Risk

        /// <summary>
        /// Self-modification validation with financial operation-specific rules
        /// Order: Risk level from highest to lowest impact for financial operations
        /// </summary>
        private Result ValidateSelfModificationRules(
            ApplicationUser actingUser,
            Account account,
            AccountModificationOperation operation)
        {
            if (!IsSelfAccess(actingUser.Id, account.UserId))
                return Result.Success(); // Not self-modification

            return operation switch
            {
                // CRITICAL RISK: Account structure modifications
                AccountModificationOperation.Edit =>
                    Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotModifyOwnAccount),
                AccountModificationOperation.Delete =>
                    Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotDeleteSelf),

                // HIGH RISK: Account status changes
                AccountModificationOperation.Freeze or AccountModificationOperation.Unfreeze =>
                    Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotFreezeOrUnfreezeOwnAccount),

                // LOW RISK: Financial transactions (allowed)
                AccountModificationOperation.Deposit or AccountModificationOperation.Withdraw =>
                    Result.Success(),

                // Default: Operation not permitted
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotModifyOwnAccount)
            };
        }

        #endregion

        #region Role Validation - Organized by Target Role Hierarchy

        /// <summary>
        /// Role validation for account ownership with financial data protection
        /// Order: Role hierarchy validation for financial account access
        /// </summary>
        private async Task<Result> ValidateAccountOwnerRoleAsync(string accountOwnerId)
        {
            var ownerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(accountOwnerId);
            return RoleHelper.IsClient(ownerRole?.Name)
                ? Result.Success()
                : Result.Forbidden(AuthorizationConstants.ErrorMessages.OnlyClientsCanBeModified);
        }

        /// <summary>
        /// Role validation for account viewing with financial data protection
        /// Order: Role hierarchy validation for financial account viewing
        /// </summary>
        private async Task<Result> ValidateAccountOwnerRoleForViewAsync(string accountOwnerId)
        {
            var ownerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(accountOwnerId);
            return RoleHelper.IsClient(ownerRole?.Name)
                ? Result.Success()
                : Result.Forbidden(AuthorizationConstants.ErrorMessages.AdminsViewClientsOnly);
        }

        /// <summary>
        /// Role validation for account creation targeting specific user types
        /// Order: Role hierarchy validation for new account creation
        /// </summary>
        private async Task<Result> ValidateTargetUserRoleForCreationAsync(string targetUserId)
        {
            var targetRole = await _uow.RoleRepository.GetRoleByUserIdAsync(targetUserId);
            return RoleHelper.IsClient(targetRole?.Name)
                ? Result.Success()
                : Result.Forbidden(AuthorizationConstants.ErrorMessages.AccountCreationAllowedForClients);
        }

        #endregion

        #region Data Filtering - Organized by Scope Complexity

        /// <summary>
        /// Apply role-based filtering with proper financial data isolation
        /// Order: Role complexity from simple to complex
        /// </summary>
        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ApplyRoleBasedFilteringAsync(
            IQueryable<Account> query,
            ApplicationRole role,
            int pageNumber,
            int pageSize)
        {
            try
            {
                // Route by role complexity
                if (RoleHelper.IsSuperAdmin(role.Name))
                {
                    return await ExecuteGlobalAccountFilteringAsync(query, pageNumber, pageSize);
                }

                if (RoleHelper.IsClient(role.Name))
                {
                    return await ExecuteSelfAccountFilteringAsync(query, pageNumber, pageSize);
                }

                return await ExecuteBankLevelAccountFilteringAsync(query, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to filter accounts", ex);
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>
                    .BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        /// <summary>
        /// Global filtering - least restrictive (SuperAdmin access to all financial accounts)
        /// </summary>
        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteGlobalAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var filteredQuery = query.OrderBy(a => a.Id);
            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        /// <summary>
        /// Self filtering - most restrictive (Client access to own accounts only)
        /// </summary>
        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteSelfAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var filteredQuery = query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id);
            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        /// <summary>
        /// Bank-level filtering - moderately restrictive (Admin access to Client accounts within bank)
        /// </summary>
        private async Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> ExecuteBankLevelAccountFilteringAsync(
            IQueryable<Account> query, int pageNumber, int pageSize)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return Result<(IEnumerable<Account> Accounts, int TotalCount)>
                    .Success((Enumerable.Empty<Account>(), 0));

            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            var filteredQuery = query
                .Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUserResult.Value!.BankId)
                .OrderBy(a => a.Id);

            var result = await _uow.AccountRepository.GetFilteredAccountsAsync(filteredQuery, pageNumber, pageSize);
            return Result<(IEnumerable<Account> Accounts, int TotalCount)>.Success(result);
        }

        /// <summary>
        /// Apply role-based query filtering for financial account access
        /// Order: Role complexity from simple to complex
        /// </summary>
        private async Task<Result<IQueryable<Account>>> ApplyRoleBasedQueryFilteringAsync(
            IQueryable<Account> query, ApplicationRole role)
        {
            try
            {
                if (RoleHelper.IsSuperAdmin(role.Name))
                {
                    return Result<IQueryable<Account>>.Success(query.OrderBy(a => a.Id));
                }

                if (RoleHelper.IsClient(role.Name))
                {
                    return Result<IQueryable<Account>>.Success(
                        query.Where(a => a.UserId == _currentUser.UserId).OrderBy(a => a.Id));
                }

                var actingUserResult = await GetActingUserAsync();
                if (actingUserResult.IsFailure)
                {
                    return Result<IQueryable<Account>>.Success(Enumerable.Empty<Account>().AsQueryable());
                }

                var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
                var filteredQuery = query
                    .Where(a => clientUserIds.Contains(a.UserId) && a.User.BankId == actingUserResult.Value!.BankId)
                    .OrderBy(a => a.Id);

                return Result<IQueryable<Account>>.Success(filteredQuery);
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to apply query filtering", ex);
                return Result<IQueryable<Account>>.BadRequest($"Failed to apply query filtering: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods - Organized by Function Category

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

            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            return actingUser.ToResult("Acting user not found.");
        }

        private async Task<Result<Account>> LoadAccountAsync(int accountId)
        {
            var spec = new AccountByIdSpecification(accountId);
            var account = await _uow.AccountRepository.FindAsync(spec);
            return account.ToResult($"Account with ID '{accountId}' not found.");
        }

        private async Task<Result<ApplicationUser>> LoadTargetUserForCreationAsync(string targetUserId)
        {
            var targetUserSpec = new UserByIdSpecification(targetUserId);
            var targetUser = await _uow.UserRepository.FindAsync(targetUserSpec);
            return targetUser.ToResult($"Target user with ID '{targetUserId}' not found.");
        }

        private async Task<Result<ApplicationRole>> GetCurrentUserRoleAsync()
        {
            try
            {
                var role = await _currentUser.GetRoleFromStoreAsync();
                return role.ToResult("Failed to retrieve user role.");
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to get user role", ex);
                return Result<ApplicationRole>.BadRequest($"Failed to get user role: {ex.Message}");
            }
        }

        // Validation Helpers
        private bool IsSelfAccess(string targetUserId) =>
            !string.IsNullOrEmpty(_currentUser.UserId) &&
            string.Equals(_currentUser.UserId, targetUserId, StringComparison.OrdinalIgnoreCase);

        private bool IsSelfAccess(string actingUserId, string targetUserId) =>
            string.Equals(actingUserId, targetUserId, StringComparison.OrdinalIgnoreCase);

        // Error Handling Helpers
        private Result HandleScopeError(Result<AccessScope> scopeResult) =>
            Result.BadRequest(scopeResult.Errors.FirstOrDefault() ?? AuthorizationConstants.ErrorMessages.SystemError);

        private Result HandleUserError(Result<ApplicationUser> userResult) =>
            Result.BadRequest(userResult.Errors.FirstOrDefault() ?? AuthorizationConstants.ErrorMessages.ResourceNotFound);

        private Result HandleAccountError(Result<Account> accountResult) =>
            Result.BadRequest(accountResult.Errors.FirstOrDefault() ?? "Account not found or inaccessible.");

        #endregion

        #region Logging Methods - Organized by Log Level and Category

        private void LogAuthorizationResult(
            AuthorizationCheckType checkType,
            string? targetId,
            Result authResult,
            AccessScope scope,
            string? additionalInfo = null)
        {
            if (authResult.IsSuccess)
            {
                _logger.LogDebug(ApiResponseMessages.Logging.AuthorizationGranted,
                    AuthorizationConstants.LoggingCategories.ACCESS_GRANTED,
                    checkType,
                    targetId,
                    scope,
                    additionalInfo ?? "N/A");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.AuthorizationDenied,
                    AuthorizationConstants.LoggingCategories.ACCESS_DENIED,
                    checkType,
                    targetId,
                    _currentUser.UserId,
                    scope,
                    string.Join(", ", authResult.Errors),
                    additionalInfo ?? "N/A");
            }
        }

        private void LogFilteringResult<T>(
            Result<T> filterResult,
            string roleName,
            int pageNumber,
            int pageSize)
        {
            if (filterResult.IsSuccess)
            {
                _logger.LogDebug(ApiResponseMessages.Logging.AccountFilteringCompleted,
                    AuthorizationConstants.LoggingCategories.AUTHORIZATION_CHECK,
                    roleName,
                    pageNumber,
                    pageSize,
                    _currentUser.UserId);
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.AccountFilteringFailed,
                    AuthorizationConstants.LoggingCategories.SYSTEM_ERROR,
                    _currentUser.UserId,
                    roleName,
                    string.Join(", ", filterResult.Errors));
            }
        }

        private void LogQueryFilteringResult<T>(Result<T> filterResult, string roleName)
        {
            if (filterResult.IsSuccess)
            {
                _logger.LogDebug(ApiResponseMessages.Logging.AccountQueryFilteringApplied,
                    AuthorizationConstants.LoggingCategories.AUTHORIZATION_CHECK,
                    roleName,
                    _currentUser.UserId);
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.AccountQueryFilteringFailed,
                    AuthorizationConstants.LoggingCategories.SYSTEM_ERROR,
                    _currentUser.UserId,
                    roleName,
                    string.Join(", ", filterResult.Errors));
            }
        }

        private void LogSelfAccessGranted(AuthorizationCheckType checkType, string targetId)
        {
            _logger.LogDebug(ApiResponseMessages.Logging.SelfAccessGranted,
                AuthorizationConstants.LoggingCategories.ACCESS_GRANTED,
                checkType,
                targetId);
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

