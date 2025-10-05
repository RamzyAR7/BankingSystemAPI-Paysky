#region Usings
using System;
#endregion


namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Constants for API response messages to maintain consistency and improve maintainability
    /// Enhanced to work seamlessly with semantic Result factory methods
    /// </summary>
    #region ApiResponseMessages
    public static class ApiResponseMessages
    {
        #region Generic
        /// <summary>
        /// Generic response messages
        /// </summary>
        public static class Generic
        {
            public const string OperationCompleted = "Operation completed successfully.";
            public const string UnknownError = "Unknown error occurred.";
        }
        #endregion

        #region ErrorPatterns
        /// <summary>
        /// Error message patterns for semantic Result factory methods
        /// These patterns are used by BaseApiController to map errors to proper HTTP status codes
        /// </summary>
        public static class ErrorPatterns
        {
            // 401 Unauthorized patterns
            public const string NotAuthenticated = "Not authenticated. Please login to access this resource.";
            public const string InvalidCredentials = "Email or password is incorrect.";
            public const string TokenExpired = "Token has expired. Please login again.";

            // 403 Forbidden patterns
            public const string AccessDenied = "Access denied. Insufficient permissions for this operation.";
            public const string MissingPermissionFormat = "Missing required permission: {0}";

            // 404 Not Found patterns
            public const string NotFound = "not found";
            public const string DoesNotExist = "does not exist";

            // 409 Conflict patterns
            public const string AlreadyExists = "already exists";
            public const string InsufficientFunds = "Insufficient funds";
            public const string AccountInactive = "account is inactive";

            // 422 Unprocessable Entity patterns
            public const string ValidationFailed = "validation failed";
            public const string BusinessRule = "business rule";
        }
        #endregion

        #region Validation
        /// <summary>
        /// Validation and request related messages
        /// </summary>
        public static class Validation
        {
            public const string RequiredDataFormat = "{0} data is required.";
            public const string RoleNameAndClaimsRequired = "Role name and claims are required.";
            public const string ResponseAlreadyStarted = "Response has already started";

            // DTO validation messages
            public const string BankNameRequired = "Bank name is required";
            public const string BankNameTooLong = "Bank name cannot exceed 100 characters";
            public const string RoleNameRequired = "Role name is required";
            public const string RoleNameLength = "Role name must be between 2 and 50 characters";

            // Common domain messages
            public const string AccountNotFound = "Account not found or inaccessible.";
            public const string AccountOwnershipRequired = "Specified user does not own this account.";
            public const string CurrencyNotFound = "Currency not found.";
            public const string CurrencyInactive = "Cannot set account to an inactive currency.";
            public const string NotFoundFormat = "{0} with ID '{1}' not found.";

            // Field level helper formats
            public const string FieldRequiredFormat = "{0} is required.";
            public const string ClaimsListRequired = "Claims list cannot be null.";

            // Feature-specific validation messages
            public const string DeleteUserHasAccounts = "Cannot delete bank with existing users.";

            // Centralized validation messages
            public const string PageNumberAndPageSizeGreaterThanZero = "Page number and page size must be greater than zero.";
            public const string BankIdGreaterThanZero = "Bank ID must be greater than zero.";
            public const string NoUserIdsProvided = "No user IDs provided.";
            public const string UsernameAlreadyExists = "Username already exists.";
            public const string EmailAlreadyExists = "Email already exists.";
            public const string AtLeastOneAccountIdRequired = "At least one account ID must be provided.";
            public const string AccountsNotFoundFormat = "Accounts with IDs [{0}] could not be found.";
            public const string CannotDeleteAccountPositiveBalanceFormat = "Cannot delete account {0} with positive balance of {1}.";
            public const string UserNotFound = "User not found.";

            // Transfer specific
            public const string TransferAmountGreaterThanZero = "Transfer amount must be greater than zero.";
            public const string SourceAndTargetAccountsMustDiffer = "Source and target accounts must be different.";

            // Additional helper formats
            public const string InvalidIdFormat = "{0} must be greater than zero.";
            public const string FieldTooLongFormat = "{0} must not exceed {1} characters.";
            public const string FieldLengthMinFormat = "{0} must be at least {1} characters long.";
            public const string FieldLengthMaxFormat = "{0} cannot exceed {1} characters.";
            public const string FieldLengthRangeFormat = "{0} must be between {1} and {2} characters long.";

            // Currency and account-specific validations
            public const string ExchangeRateGreaterThanZero = "Exchange rate must be greater than zero.";
            public const string OverdraftLimitNonNegative = "Overdraft limit must be non-negative.";

            // Password and confirmation messages
            public const string PasswordComplexity = "New password must contain at least one lowercase letter, one uppercase letter, and one digit.";
            public const string PasswordConfirmationMismatch = "Password confirmation must match the new password.";

            // Additional validation messages
            public const string InvalidEmailAddress = "Invalid email address.";
            public const string DepositAmountGreaterThanZero = "Deposit amount must be greater than zero.";
            public const string InitialBalanceNonNegative = "Initial balance must be non-negative.";

            // New user-related validation messages
            public const string FullNameLettersOnly = "Full name can only contain letters and spaces.";
            public const string AgeRange = "User must be at least 18 years old and not older than 100 years.";
            public const string InvalidPhoneNumberFormat = "Invalid phone number format. Must be 11-15 digits, optionally starting with +.";
            public const string NationalIdDigits = "National ID must contain only digits.";

            // Currency messages
            public const string AnotherBaseCurrencyExists = "Another base currency already exists. Clear it before setting this currency as base.";

            // New validation message for protected role deletion
            public const string ProtectedRoleDeletionNotAllowed = "Cannot delete protected system roles (SuperAdmin, Admin, Client).";

            // New validation messages for user roles
            public const string NotAuthorizedToAssignSuperAdmin = "Not authorized to assign SuperAdmin role.";
            public const string RoleCannotBeEmptyOrWhitespace = "Role cannot be empty or whitespace.";

            // User-related messages
            public const string UserRequestCannotBeNull = "User request cannot be null.";
            public const string UserIdRequired = "User ID is required.";
            public const string UserIdInvalidFormat = "User ID must be a valid format.";
            public const string UserIdsCollectionCannotBeNull = "User IDs collection cannot be null.";
            public const string AtLeastOneUserIdProvided = "At least one user ID must be provided.";
            public const string AllUserIdsMustBeValid = "All user IDs must be valid (non-empty).";
            public const string DuplicateUserIdsNotAllowed = "Duplicate user IDs are not allowed.";
            public const string OrderDirectionMustBeAscOrDesc = "Order direction must be 'ASC' or 'DESC'.";

            // Password change related
            public const string CurrentPasswordRequired = "Current password is required to change your password.";
            public const string PasswordsDoNotMatch = "The new password and confirmation password do not match. Please ensure both passwords are identical.";
            public const string NotAuthorizedToChangePassword = "You are not authorized to change this user's password.";
            public const string IncorrectCurrentPassword = "The current password is incorrect. Please check your password and try again.";

            // Role related
            public const string RoleRequiredForSuperAdmin = "Role is required for SuperAdmin users.";
            public const string RoleNameCannotExceed = "Role name cannot exceed 100 characters.";
            public const string RoleNameInvalidFormat = "Role name can only contain letters, numbers, and spaces.";

            // SuperAdmin protection
            public const string SuperAdminCannotBeDeactivated = "SuperAdmin users cannot be deactivated.";

            // Bank id
            public const string BankIdMustBeGreaterThanZero = "Bank ID must be greater than 0.";

            // Bank ids
            // Bulk delete user messages
            public const string CannotDeleteSelfBulk = "Cannot delete yourself in bulk delete operation.";
            public const string NoValidUsersFoundToDelete = "No valid users found to delete.";
            public const string UserConflictExistsFormat = "Another user with conflicting {0} already exists in this bank.";

            // Savings account specific messages
            public const string UserInactive = "Cannot create account for inactive user.";
            public const string InterestRateRange = "Interest rate must be between 0% and 100%.";
        }
        #endregion

        #region Infrastructure
        /// <summary>
        /// Messages used by middleware and infrastructure layers
        /// </summary>
        public static class Infrastructure
        {
            public const string ConcurrencyConflict = "A concurrency conflict occurred. Please refresh and try again.";
            public const string RequestTimedOut = "The request timed out. Please try again.";
            public const string AccessDeniedAuthenticate = "Access denied. Please authenticate and try again.";
            public const string InvalidRequestParametersFormat = "Invalid request parameters: {0}";
            public const string InvalidOperation = "The requested operation is not valid in the current state.";
            public const string InvalidJsonFormat = "Invalid JSON format in request.";
            public const string SystemHighLoad = "The system is experiencing high load. Please try again later.";
            public const string SystemErrorContact = "A system error occurred. Please contact support.";
            public const string ExternalServiceUnavailable = "External service unavailable. Please try again later.";
            public const string RequestCancelled = "The request was cancelled or timed out.";
            public const string OperationCancelled = "The operation was cancelled.";
            public const string RequiredResourceNotFound = "A required resource was not found on the server.";
            public const string RequiredFileNotFound = "A required file was not found on the server.";
            public const string UnexpectedErrorDetailed = "An unexpected error occurred. Please try again or contact support if the problem persists.";

            // Rate limiting
            public const string RateLimitExceeded = "Too many requests. Please try again later.";

            // Cache related messages
            public const string CacheMiss = "Cache miss";
            public const string CacheAccessErrorFormat = "Cache access error: {0}";
            public const string CacheSetErrorFormat = "Cache set error: {0}";
            public const string CacheRemovalErrorFormat = "Cache removal error: {0}";
            public const string CacheSetSuccess = "Cache set successfully";

            // Job related messages
            public const string JobStoppingDueToCancellation = "{0} stopping due to cancellation.";
            public const string JobRunFailedFormat = "{0} run failed";

            // Job console/log formats
            public const string JobStartedFormat = "[{0}] started at {1:u}";
            public const string JobRunStartedFormat = "[{0}] run started at {1:u}";
            public const string JobAppliedInterestFormat = "[{0}] Applied interest {1} to Savings Id={2}, Number={3} at {4:u}";
            public const string JobErrorProcessingAccountFormat = "[{0}] Error processing account Id={1}: {2}";
            public const string JobRunCompletedFormat = "[{0}] run completed. TotalAccounts={1}, Applied={2} at {3:u}, Duration={4}s";

            // Request timing messages
            public const string RequestTimingLogFormat = "RequestTiming: {Method} {Path} responded {StatusCode} in {Elapsed} ms";
            public const string RequestTimingConsoleFormat = "[Timing] {0:u} - {1} {2} => {3} in {4} ms";

            // Database specific messages
            public const string DbUniqueConstraintViolation = "A record with the same information already exists.";
            public const string DbForeignKeyViolation = "The operation violates data relationships. Please check related records.";
            public const string DbCheckConstraintViolation = "The operation violates data validation rules.";
            public const string DbNotNullViolation = "Required information is missing. Please provide all required fields.";
            public const string DbGenericError = "A database error occurred while processing your request.";
        }
        #endregion

        #region Delete
        /// <summary>
        /// Delete operation messages
        /// </summary>
        public static class Delete
        {
            #region User
            public static class User
            {
                public const string Success = "User has been successfully deleted from the system.";
                public const string Deactivated = "User account has been successfully deactivated.";
            }
            #endregion

            #region Bank
            public static class Bank
            {
                public const string Success = "Bank has been successfully removed from the system.";
            }
            #endregion

            #region Role
            public static class Role
            {
                public const string Success = "Role has been successfully deleted and removed from all assignments.";
            }
            #endregion

            #region Account
            public static class Account
            {
                public const string Success = "Account has been successfully closed and removed.";
            }
            #endregion

            #region Currency
            public static class Currency
            {
                public const string Success = "Currency has been successfully removed from the system.";
            }
            #endregion

            #region CheckingAccount
            public static class CheckingAccount
            {
                public const string Success = "Checking account has been successfully deleted.";
            }
            #endregion

            #region SavingsAccount
            public static class SavingsAccount
            {
                public const string Success = "Savings account has been successfully deleted.";
            }
            #endregion

            #region Generic
            public static class Generic
            {
                public const string Success = "Resource has been successfully deleted.";
            }
            #endregion
        }
        #endregion

        #region Update
        /// <summary>
        /// Update operation messages
        /// </summary>
        public static class Update
        {
            #region Password
            public static class Password
            {
                public const string Success = "Password has been successfully changed.";
            }
            #endregion

            #region User
            public static class User
            {
                public const string Success = "User information has been successfully updated.";
                public const string ProfileSuccess = "User profile has been successfully updated.";
                public const string ContactSuccess = "Contact information has been successfully updated.";
                public const string RoleSuccess = "User role has been successfully updated.";
            }
            #endregion

            #region Bank
            public static class Bank
            {
                public const string Success = "Bank details have been successfully updated.";
                public const string BranchSuccess = "Bank branch information has been successfully updated.";
                public const string SwiftSuccess = "SWIFT code has been successfully updated.";
                public const string RoutingSuccess = "Routing number has been successfully updated.";
                public const string AddressSuccess = "Bank address has been successfully updated.";
            }
            #endregion

            #region Role
            public static class Role
            {

                public const string Success = "Role has been successfully updated.";
                public const string PermissionsSuccess = "Permissions have been successfully updated.";
                public const string AssignmentSuccess = "Role assignment has been successfully updated.";
            }
            #endregion

            #region Account
            public static class Account
            {
                public const string Success = "Account information has been successfully updated.";
                public const string BalanceSuccess = "Account balance has been successfully updated.";
                public const string LimitsSuccess = "Account limits have been successfully updated.";
                public const string TypeSuccess = "Account type has been successfully changed.";
                public const string InterestSuccess = "Interest rate has been successfully updated.";
                public const string OverdraftSuccess = "Overdraft settings have been successfully updated.";
                public const string BeneficiarySuccess = "Beneficiary information has been successfully updated.";
            }
            #endregion

            #region Currency
            public static class Currency
            {
                public const string Success = "Currency has been successfully updated.";
                public const string ExchangeRateSuccess = "Exchange rate has been successfully updated.";
                public const string SymbolSuccess = "Currency symbol has been successfully updated.";
            }
            #endregion

            #region CheckingAccount
            public static class CheckingAccount
            {
                public const string Success = "Checking account has been successfully updated.";
            }
            #endregion

            #region SavingsAccount
            public static class SavingsAccount
            {
                public const string Success = "Savings account has been successfully updated.";
            }
            #endregion

            #region Generic
            public static class Generic
            {
                public const string Success = "Resource has been successfully updated.";
                public const string SecuritySuccess = "Security settings have been successfully updated.";
                public const string ProfileSuccess = "Profile information has been successfully updated.";
            }
            #endregion
        }
        #endregion

        #region Status
        /// <summary>
        /// Active/Inactive status messages
        /// </summary>
        public static class Status
        {
            #region User
            public static class User
            {
                public const string Activated = "User has been successfully activated.";
                public const string Deactivated = "User has been successfully deactivated.";
            }
            #endregion

            #region Bank
            public static class Bank
            {
                public const string Activated = "Bank has been successfully activated.";
                public const string Deactivated = "Bank has been successfully deactivated.";
            }
            #endregion

            #region Account
            public static class Account
            {
                public const string Activated = "Account has been successfully activated.";
                public const string Deactivated = "Account has been successfully deactivated.";
            }
            #endregion

            #region Currency
            public static class Currency
            {
                public const string Activated = "Currency has been successfully activated.";
                public const string Deactivated = "Currency has been successfully deactivated.";
            }
            #endregion

            #region Generic
            public static class Generic
            {
                public const string Activated = "Resource has been successfully activated.";
                public const string Deactivated = "Resource has been successfully deactivated.";
            }
            #endregion
        }
        #endregion

        #region Authentication
        /// <summary>
        /// Authentication and processing messages
        /// </summary>
        public static class Authentication
        {
            public const string LoginSuccess = "User has been successfully logged in.";
            public const string LogoutSuccess = "User has been successfully logged out.";
            public const string TokenRefreshed = "Authentication token has been successfully refreshed.";
            public const string TokenRevoked = "User token has been successfully revoked.";
            public const string OperationSuccess = "Authentication operation completed successfully.";
        }
        #endregion

        #region Transaction
        /// <summary>
        /// Transaction processing messages
        /// </summary>
        public static class Transaction
        {
            public const string DepositSuccess = "Deposit transaction has been successfully processed.";
            public const string WithdrawSuccess = "Withdrawal transaction has been successfully processed.";
            public const string TransferSuccess = "Transfer transaction has been successfully completed.";
            public const string ProcessedSuccess = "Transaction has been processed successfully.";
        }
        #endregion

        #region RolePermissions
        /// <summary>
        /// Role and permission messages
        /// </summary>
        public static class RolePermissions
        {
            public const string AssignmentSuccess = "Role permissions have been successfully assigned.";
            public const string UpdateSuccess = "User role has been successfully updated.";
        }
        #endregion

        #region Processing
        /// <summary>
        /// General processing messages
        /// </summary>
        public static class Processing
        {
            public const string OperationSuccess = "Operation has been processed successfully.";
        }
        #endregion

        #region BankingErrors
        /// <summary>
        /// Banking-specific error messages that align with Result factory methods
        /// </summary>
        public static class BankingErrors
        {
            /// <summary>
            /// Format: "Insufficient funds. Requested: {0}, Available: {1}"
            /// Used by Result.InsufficientFunds()
            /// </summary>
            public const string InsufficientFundsFormat = "Insufficient funds. Requested: {0}, Available: {1}";

            /// <summary>
            /// Format: "Account {0} is inactive and cannot be used for transactions"
            /// Used by Result.AccountInactive()
            /// </summary>
            public const string AccountInactiveFormat = "Account {0} is inactive and cannot be used for transactions";

            /// <summary>
            /// Format: "{0} with identifier '{1}' already exists"
            /// Used by Result.AlreadyExists()
            /// </summary>
            public const string AlreadyExistsFormat = "{0} with identifier '{1}' already exists";

            /// <summary>
            /// Format: "{0} with id '{1}' was not found"
            /// Used by Result.NotFound()
            /// </summary>
            public const string NotFoundFormat = "{0} with id '{1}' was not found";

            /// <summary>
            /// Specific message for transfer authorization when source is not client-owned
            /// </summary>
            public const string TransfersFromClientsOnly = "Transfers can only be initiated from Client-owned accounts.";
        }
        #endregion

        #region Logging
        /// <summary>
        /// Centralized structured logging templates
        /// </summary>
        public static class Logging
        {
            // Authorization
            public const string AuthorizationGranted = "{LogCategory} Account authorization granted: CheckType={CheckType}, TargetId={TargetId}, Scope={Scope}, Info={Info}";
            public const string AuthorizationDenied = "{LogCategory} Account authorization denied: CheckType={CheckType}, TargetId={TargetId}, ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}, Info={Info}";

            // Generic authorization templates
            public const string AuthorizationGrantedGeneric = "{LogCategory} Authorization granted: CheckType={CheckType}, TargetId={TargetId}, Scope={Scope}, Info={Info}";
            public const string AuthorizationDeniedGeneric = "{LogCategory} Authorization denied: CheckType={CheckType}, TargetId={TargetId}, ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}, Info={Info}";

            // Middleware
            public const string MiddlewareRequestCompleted = "[MIDDLEWARE] Request completed successfully: RequestId={RequestId}, Path={Path}, Method={Method}, StatusCode={StatusCode}";
            public const string MiddlewareExceptionHandled = "[MIDDLEWARE] Exception handled: RequestId={RequestId}, Path={Path}, Method={Method}, ExceptionType={ExceptionType}, Message={Message}";
            public const string MiddlewareExceptionProcessingFailed = "[MIDDLEWARE] Failed to process exception: RequestId={RequestId}, Errors={Errors}";
            public const string MiddlewareCriticalProcessingError = "[MIDDLEWARE] Critical error in exception processing: RequestId={RequestId}";

            // Filtering
            public const string AccountFilteringCompleted = "{LogCategory} Account filtering completed: Role={Role}, Page={Page}, Size={Size}, ActingUserId={ActingUserId}";
            public const string AccountFilteringFailed = "{LogCategory} Account filtering failed: ActingUserId={ActingUserId}, Role={Role}, Errors={Errors}";
            public const string AccountQueryFilteringApplied = "{LogCategory} Account query filtering applied: Role={Role}, ActingUserId={ActingUserId}";
            public const string AccountQueryFilteringFailed = "{LogCategory} Account query filtering failed: ActingUserId={ActingUserId}, Role={Role}, Errors={Errors}";

            // User filtering (generic) - added for user authorization logs
            public const string UserFilteringCompleted = "{LogCategory} User filtering completed: Scope={Scope}, Page={Page}, Size={Size}, ActingUserId={ActingUserId}";
            public const string UserFilteringFailed = "{LogCategory} User filtering failed: ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}";
            public const string UserQueryFilteringApplied = "{LogCategory} User query filtering applied: Scope={Scope}, ActingUserId={ActingUserId}";
            public const string UserQueryFilteringFailed = "{LogCategory} User query filtering failed: ActingUserId={ActingUserId}, Scope={Scope}, Errors={Errors}";

            // Self access
            public const string SelfAccessGranted = "{LogCategory} Self-access granted: CheckType={CheckType}, AccountId={AccountId}";

            // Authorization helper
            public const string RoleCheck = "[AUTHORIZATION] Role check: {Role} == {ExpectedRole} = {IsMatch}";
            public const string BankAdminCheckFailed = "[AUTHORIZATION] Bank admin check failed - null/empty role";
            public const string BankAdminCheck = "[AUTHORIZATION] Bank admin check: Role={Role}, IsBankAdmin={IsBankAdmin}";
            public const string RoleValidationSuccessful = "[AUTHORIZATION] Role validation successful: {Role} validated against {ExpectedRole}";

            // Cache
            public const string CacheHit = "[CACHE] Cache hit: Key={Key}, Type={Type}";
            public const string CacheMiss = "[CACHE] Cache miss: Key={Key}, Type={Type}";
            public const string CacheAccessFailed = "[CACHE] Cache access failed: Key={Key}, Type={Type}";
            public const string CacheSetFailed = "[CACHE] Failed to set cache: Key={Key}, Type={Type}";
            public const string CacheRemoveFailed = "[CACHE] Failed to remove cache: {Key}";

            // Request/response
            public const string IncomingRequest = "Incoming request:\n{Request}";
            public const string OutgoingResponse = "Outgoing response:\n{Response}";
            public const string ResponseSummary = "Response summary: StatusCode={StatusCode}, Username={Username}, Roles={Roles}, UserId={UserId}";
            public const string ExceptionOccurred = "Exception occurred during request execution: {Message}";

            // Controller operation messages
            public const string OperationCompletedController = "Operation completed successfully. Controller: {Controller}, Action: {Action}";
            public const string OperationFailedController = "Operation failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}";

            // Role service specific
            public const string RoleRetrieved = "Retrieved {Count} roles with claims successfully";
            public const string RoleRetrieveFailed = "Failed to retrieve roles. Errors: {Errors}";
            public const string RoleRetrieveError = "Failed to retrieve roles: {Message}";

            public const string RoleCreated = "Role created successfully: {RoleName}";
            public const string RoleCreateFailed = "Role creation failed for: {RoleName}. Errors: {Errors}";

            public const string RoleDeleted = "Role deleted successfully: RoleId={RoleId}, Name={RoleName}";
            public const string RoleDeleteFailed = "Role deletion failed for: {RoleId}. Errors: {Errors}";

            public const string RoleUsageCheckCompleted = "Role usage check completed: RoleId={RoleId}, IsInUse={IsInUse}, ViaFK={ViaFK}, ViaRoleTable={ViaRoleTable}";
            public const string RoleUsageCheckFailed = "Role usage check failed: RoleId={RoleId}";

            public const string RoleFkCheckFailed = "Failed to check FK usage for role: {RoleId}";
            public const string RoleTableCheckFailed = "Failed to check UserRoles table usage for role: {RoleId}";

            // User service specific
            public const string UserRetrievedByUsername = "User retrieved successfully by username: {Username}";
            public const string UserRetrieveByUsernameFailed = "Failed to retrieve user by username: {Username}, Errors: {Errors}";

            public const string UserRetrievedById = "User retrieved successfully by ID: {UserId}";
            public const string UserRetrieveByIdFailed = "Failed to retrieve user by ID: {UserId}, Errors: {Errors}";

            public const string UserRetrievedByEmail = "User retrieved successfully by email: {Email}";
            public const string UserRetrieveByEmailFailed = "Failed to retrieve user by email: {Email}, Errors: {Errors}";

            public const string UserRoleRetrieved = "User role retrieved successfully for ID: {UserId}";
            public const string UserRoleRetrieveFailed = "Failed to retrieve user role for ID: {UserId}, Errors: {Errors}";

            public const string UsersRetrievedForBankId = "Retrieved {Count} users for bank ID: {BankId}";
            public const string UsersRetrievedForBankName = "Retrieved {Count} users for bank name: {BankName}";

            public const string BankActiveStatusChecked = "Bank active status checked for ID: {BankId}, IsActive: {IsActive}";

            public const string UserCreationFailedUsernameExists = "User creation failed - username exists: {Username}";
            public const string UserCreationFailedEmailExists = "User creation failed - email exists: {Email}";
            public const string UserCreationFailedRoleNotFound = "User creation failed - role not found: {Role}";
            public const string UserCreationFailed = "User creation failed for {Username}: {Errors}";
            public const string UserCreated = "User created successfully: {Username}";

            public const string UserUpdated = "User updated successfully: {UserId}";
            public const string UserUpdateFailed = "User update failed for {UserId}: {Errors}";

            // Savings account logging
            public const string SavingsAccountCreated = "Savings account created successfully: AccountNumber={AccountNumber}, UserId={UserId}, Currency={Currency}";
            public const string SavingsAccountCreateFailed = "Savings account creation failed for UserId={UserId}: {Errors}";
            public const string SavingsAccountUpdated = "Savings account updated successfully: AccountId={AccountId}, UserId={UserId}, CurrencyId={CurrencyId}";
            public const string SavingsAccountUpdateFailed = "Savings account update failed: AccountId={AccountId}, UserId={UserId}, Errors={Errors}";

            public const string PasswordChangeFailed = "Password change failed for {UserId}: {Errors}";
            public const string PasswordChanged = "Password changed successfully for user: {UserId}";

            public const string UserDeleted = "User deleted successfully: {UserId}";
            public const string UserDeletionFailed = "User deletion failed for {UserId}: {Errors}";

            public const string BulkUserDeletionCompleted = "Bulk user deletion completed for {Count} users";
            public const string BulkUserDeletionFailed = "Bulk user deletion failed for {UserId}: {Errors}";

            public const string SetUserActiveStatusFailed = "Set user active status failed for {UserId}: {Errors}";
            public const string SetUserActiveStatusChanged = "User active status changed to {IsActive} for user: {UserId}";

            // RoleClaims service specific
            public const string RoleClaimsUpdated = "Role claims updated successfully for role: {RoleName}";
            public const string RoleClaimsUpdateFailed = "Role claims update failed for role: {RoleName}. Errors: {Errors}";

            public const string RoleClaimsRetrieved = "Successfully retrieved all claims by group";
            public const string RoleClaimsRetrieveFailed = "Failed to retrieve claims by group. Errors: {Errors}";

            // Job / Batch processing
            public const string BatchProcessed = "Batch {BatchNumber} processed {BatchCount} accounts, applied {BatchApplied} interest, duration {Duration}s";

            // Cleanup job
            public const string CleanupJobBatch = "CleanupJob Batch {BatchNumber} removed {BatchRemoved} tokens, duration {Duration}s";
            public const string CleanupJobRunCompleted = "CleanupJob run completed. TotalTokens={Total}, Removed={Removed}, Duration={Duration}s";

            // Transaction logging
            public const string TransactionTransferSuccess = "Transfer successful. Source: {SourceId}, Target: {TargetId}, Amount: {Amount}";
            public const string TransactionTransferFailed = "Transfer failed. Source: {SourceId}, Target: {TargetId}, Amount: {Amount}, Errors: {Errors}";
            public const string TransactionLoadAccountsFailed = "Failed to load accounts for transfer: Source={SourceId}, Target={TargetId}";
            public const string TransactionValidateBanksFailed = "Failed to validate banks for transfer: {Message}";

            public const string TransactionDepositSuccess = "Deposit successful. Account: {AccountId}, Amount: {Amount}";
            public const string TransactionDepositFailed = "Deposit failed. Account: {AccountId}, Amount: {Amount}, Errors: {Errors}";
            public const string TransactionDepositExecutionFailed = "Deposit failed during execution: Account={AccountId}, Error={Error}";

            // Validation pipeline
            public const string ValidationPipelineReturningFailure = "[VALIDATION_PIPELINE] Returning validation failure Result<{GenericType}> for: {RequestType}";
            public const string ValidationPipelineUnsupportedResponse = "[VALIDATION_PIPELINE] Unsupported response type {ResponseType} for validation failure in request: {RequestType}";
            // New validation pipeline logs
            public const string ValidationPipelineNoValidators = "[VALIDATION_PIPELINE] No validators found for request type: {RequestType}";
            public const string ValidationPipelinePassed = "[VALIDATION_PIPELINE] Validation passed for request type: {RequestType}, Validators: {ValidatorCount}";
            public const string ValidationPipelineFailed = "[VALIDATION_PIPELINE] Validation failed for request type: {RequestType}, Errors: {Errors}, ValidatorCount: {ValidatorCount}";
            public const string ValidationPipelineException = "[VALIDATION_PIPELINE] Exception during validation for request type: {RequestType}";

            // Seeding
            public const string SeedingCompleted = "Seeding data completed.";
            public const string SeedingNoRolesFound = "Role seeding completed but no roles were found. Skipping dependent seeders.";
            public const string SeedingFailed = "Seeding failed: {Message}";

            // Scope resolution
            public const string ScopeResolved = "[AUTHORIZATION] Access scope resolved: UserId={UserId}, Role={Role}, Scope={Scope}";
            public const string ScopeResolveFailed = "[AUTHORIZATION] Failed to resolve access scope for user: {UserId}, Error={Error}";
        }
        #endregion
    }
    #endregion
}
