#region Usings
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.AuthorizationServices
{
    /// <summary>
    /// Comprehensive transaction authorization service implementing hierarchical access control
    /// with organized validation rules and proper error handling for financial transaction operations
    /// </summary>
    public class TransactionAuthorizationService : ITransactionAuthorizationService
    {

        #region Private Fields - Organized by Responsibility

        // Core Dependencies
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IScopeResolver _scopeResolver;
        private readonly ILogger<TransactionAuthorizationService> _logger;

        #endregion

        #region Constructor

        public TransactionAuthorizationService(
            ICurrentUserService currentUser,
            IUnitOfWork uow,
            IScopeResolver scopeResolver,
            ILogger<TransactionAuthorizationService> logger)
        {
            _currentUser = currentUser;
            _uow = uow;
            _scopeResolver = scopeResolver;
            _logger = logger;
        }

        #endregion

        #region Public Interface Implementation - Ordered by Operation Impact

        /// <summary>
        /// Validates money transfer initiation with high security impact
        /// Priority: High impact operation for financial money movement
        /// </summary>
        public async Task<Result> CanInitiateTransferAsync(int sourceAccountId, int targetAccountId)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return HandleScopeError(scopeResult);

            var sourceAccountResult = await LoadSourceAccountAsync(sourceAccountId);
            if (sourceAccountResult.IsFailure)
                return HandleAccountError(sourceAccountResult);

            var authorizationResult = await ValidateTransferAuthorizationAsync(
                sourceAccountResult.Value!, 
                scopeResult.Value);
            
            LogAuthorizationResult(
                AuthorizationCheckType.Modify, 
                $"Transfer: {sourceAccountId} -> {targetAccountId}", 
                authorizationResult, 
                scopeResult.Value, 
                "MoneyTransfer");

            return authorizationResult;
        }

        /// <summary>
        /// Filters transaction history based on authorization scope
        /// Priority: Low-Medium impact operation with potential high financial data exposure
        /// </summary>
        public async Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> FilterTransactionsAsync(
            IQueryable<Transaction> query, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            var scopeResult = await GetScopeAsync();
            if (scopeResult.IsFailure)
                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Failure(scopeResult.ErrorItems);

            var filteringResult = await ApplyScopeFilteringAsync(query, scopeResult.Value, pageNumber, pageSize);

             LogFilteringResult(filteringResult, scopeResult.Value, pageNumber, pageSize);

             return filteringResult;
         }

         #endregion

        #region Core Authorization Logic - Organized by Validation Type

        /// <summary>
        /// Validates transfer authorization with hierarchical scope checking for financial operations
        /// Order: Self-access → Scope-based validation → Financial operation protection
        /// </summary>
        private async Task<Result> ValidateTransferAuthorizationAsync(Account sourceAccount, AccessScope scope)
        {
            // Critical Priority: Financial operations require strict validation
            return scope switch
            {
                // Least restrictive: Global access for system administrators
                AccessScope.Global => Result.Success(),
                
                // Most restrictive: Self-access only for clients
                AccessScope.Self => ValidateSelfTransferAuthorization(sourceAccount),
                
                // Moderately restrictive: Bank-level access with role restrictions
                AccessScope.BankLevel => await ValidateBankLevelTransferAsync(sourceAccount),
                
                // Default: Unknown scope
                _ => Result.Forbidden(AuthorizationConstants.ErrorMessages.UnknownAccessScope)
            };
        }

        #endregion

        #region Bank-Level Authorization - Organized by Security Layer

        /// <summary>
        /// Self-transfer validation - users can only initiate transfers from their own accounts
        /// Order: Account ownership validation → Financial operation authorization
        /// </summary>
        private Result ValidateSelfTransferAuthorization(Account sourceAccount)
        {
            // Layer 1: Account ownership validation for financial security
            if (!IsSelfAccess(sourceAccount.UserId))
            {
                return Result.Forbidden(AuthorizationConstants.ErrorMessages.CannotUseOthersAccounts);
            }

            LogSelfAccessGranted(AuthorizationCheckType.Modify, sourceAccount.Id.ToString());
            return Result.Success();
        }

        /// <summary>
        /// Bank-level transfer validation with comprehensive security checks for financial operations
        /// Order: Source account owner role → Bank isolation → Financial operation validation
        /// </summary>
        private async Task<Result> ValidateBankLevelTransferAsync(Account sourceAccount)
        {
            // Layer 1: Acting user validation
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return actingUserResult;

            // Layer 2: Role-based access validation for financial operations
            var roleValidationResult = await ValidateSourceAccountOwnerRoleAsync(sourceAccount.UserId);
            if (roleValidationResult.IsFailure)
                return roleValidationResult;

            // Layer 3: Bank isolation validation for financial security
            return BankGuard.ValidateSameBank(actingUserResult.Value!.BankId, sourceAccount.User?.BankId);
        }

        #endregion

        #region Role Validation - Organized by Financial Security Requirements

        /// <summary>
        /// Role validation for source account ownership in financial transfers
        /// Order: Role hierarchy validation for financial transaction authorization
        /// </summary>
        private async Task<Result> ValidateSourceAccountOwnerRoleAsync(string sourceUserId)
        {
            var sourceOwnerRole = await _uow.RoleRepository.GetRoleByUserIdAsync(sourceUserId);
            return RoleHelper.IsClient(sourceOwnerRole?.Name)
                ? Result.Success()
                : Result.Forbidden(ApiResponseMessages.BankingErrors.TransfersFromClientsOnly);
        }

        #endregion

        #region Data Filtering - Organized by Scope Complexity

        /// <summary>
        /// Apply scope-based filtering with proper financial transaction data isolation
        /// Order: Scope complexity from simple to complex for transaction history
        /// </summary>
        private async Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> ApplyScopeFilteringAsync(
            IQueryable<Transaction> query, 
            AccessScope scope, 
            int pageNumber, 
            int pageSize)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;

                // Materialize query to an in-memory list to avoid expression tree/executor differences
                var allItems = query.ToList();

                IEnumerable<Transaction> filteredItems = scope switch
                {
                    AccessScope.Global => allItems,
                    AccessScope.Self => allItems.Where(t => t.AccountTransactions != null && t.AccountTransactions.Any(at => at.Account != null && string.Equals(at.Account.UserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))),
                    AccessScope.BankLevel => allItems.Where(t => t.AccountTransactions != null && t.AccountTransactions.Any(at => at.Account != null && at.Account.User != null && at.Account.User.BankId == _currentUser.BankId)),
                    _ => Enumerable.Empty<Transaction>()
                };

                var ordered = filteredItems.OrderByDescending(t => t.Timestamp).ToList();
                var total = ordered.Count;
                var items = ordered.Skip(skip).Take(pageSize).ToList();

                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((items, total));
            }
            catch (Exception ex)
            {
                LogSystemError("Failed to filter transactions", ex);
                return Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                    .BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        /// <summary>
        /// Bank-level transaction filtering - moderately restrictive (Admin access to Client transactions within bank)
        /// </summary>
        private async Task<Result<IQueryable<Transaction>>> ApplyBankLevelTransactionFilterAsync(IQueryable<Transaction> query)
        {
            var actingUserResult = await GetActingUserAsync();
            if (actingUserResult.IsFailure)
                return Result<IQueryable<Transaction>>.Success(query.Where(_ => false));

            // Filter to show only transactions involving Client users within the same bank
            var clientUserIds = _uow.RoleRepository.UsersWithRoleQuery(UserRole.Client.ToString());
            var filteredQuery = query.Where(t => 
                t.AccountTransactions.Any(at => 
                    clientUserIds.Contains(at.Account.UserId) && 
                    at.Account.User.BankId == actingUserResult.Value!.BankId));

            return Result<IQueryable<Transaction>>.Success(filteredQuery);
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
                return Result<AccessScope>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Result<ApplicationUser>> GetActingUserAsync()
        {
            var actingUserSpec = new UserByIdSpecification(_currentUser.UserId);
            var actingUser = await _uow.UserRepository.FindAsync(actingUserSpec);
            return actingUser.ToResult(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, "Acting user", _currentUser.UserId));
        }

        private async Task<Result<Account>> LoadSourceAccountAsync(int sourceAccountId)
        {
            var srcSpec = new Specifications.AccountSpecification.AccountByIdSpecification(sourceAccountId);
            var source = await _uow.AccountRepository.FindAsync(srcSpec);
            // Use explicit message format expected by unit tests
            return source.ToResult($"Source account with ID '{sourceAccountId}' not found.");
        }

        // Validation Helpers
        private bool IsSelfAccess(string targetUserId) => 
            _currentUser.UserId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase);

        // Error Handling Helpers
        private Result HandleScopeError(Result<AccessScope> scopeResult) => 
            Result.BadRequest(scopeResult.ErrorItems.FirstOrDefault()?.Message ?? AuthorizationConstants.ErrorMessages.SystemError);

        private Result HandleAccountError(Result<Account> accountResult) => 
            Result.BadRequest(accountResult.ErrorItems.FirstOrDefault()?.Message ?? ApiResponseMessages.Validation.AccountNotFound);

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
                _logger.LogDebug(ApiResponseMessages.Logging.AuthorizationGrantedGeneric,
                    AuthorizationConstants.LoggingCategories.ACCESS_GRANTED,
                    checkType,
                    targetId,
                    scope,
                    additionalInfo ?? "N/A");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.AuthorizationDeniedGeneric,
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

