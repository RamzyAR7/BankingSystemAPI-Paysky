using System;

namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Constants for API response messages to maintain consistency and improve maintainability
    /// Enhanced to work seamlessly with semantic Result factory methods
    /// </summary>
    public static class ApiResponseMessages
    {
        /// <summary>
        /// Generic response messages
        /// </summary>
        public static class Generic
        {
            public const string OperationCompleted = "Operation completed successfully.";
            public const string UnknownError = "Unknown error occurred.";
        }

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

        /// <summary>
        /// Delete operation messages
        /// </summary>
        public static class Delete
        {
            public static class User
            {
                public const string Success = "User has been successfully deleted from the system.";
                public const string Deactivated = "User account has been successfully deactivated.";
            }

            public static class Bank
            {
                public const string Success = "Bank has been successfully removed from the system.";
            }

            public static class Role
            {
                public const string Success = "Role has been successfully deleted and removed from all assignments.";
            }

            public static class Account
            {
                public const string Success = "Account has been successfully closed and removed.";
            }

            public static class Currency
            {
                public const string Success = "Currency has been successfully removed from the system.";
            }

            public static class CheckingAccount
            {
                public const string Success = "Checking account has been successfully deleted.";
            }

            public static class SavingsAccount
            {
                public const string Success = "Savings account has been successfully deleted.";
            }

            public static class Generic
            {
                public const string Success = "Resource has been successfully deleted.";
            }
        }

        /// <summary>
        /// Update operation messages
        /// </summary>
        public static class Update
        {
            public static class Password
            {
                public const string Success = "Password has been successfully changed.";
            }

            public static class User
            {
                public const string Success = "User information has been successfully updated.";
                public const string ProfileSuccess = "User profile has been successfully updated.";
                public const string ContactSuccess = "Contact information has been successfully updated.";
                public const string RoleSuccess = "User role has been successfully updated.";
            }

            public static class Bank
            {
                public const string Success = "Bank details have been successfully updated.";
                public const string BranchSuccess = "Bank branch information has been successfully updated.";
                public const string SwiftSuccess = "SWIFT code has been successfully updated.";
                public const string RoutingSuccess = "Routing number has been successfully updated.";
                public const string AddressSuccess = "Bank address has been successfully updated.";
            }

            public static class Role
            {
                public const string Success = "Role has been successfully updated.";
                public const string PermissionsSuccess = "Permissions have been successfully updated.";
                public const string AssignmentSuccess = "Role assignment has been successfully updated.";
            }

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

            public static class Currency
            {
                public const string Success = "Currency has been successfully updated.";
                public const string ExchangeRateSuccess = "Exchange rate has been successfully updated.";
                public const string SymbolSuccess = "Currency symbol has been successfully updated.";
            }

            public static class CheckingAccount
            {
                public const string Success = "Checking account has been successfully updated.";
            }

            public static class SavingsAccount
            {
                public const string Success = "Savings account has been successfully updated.";
            }

            public static class Generic
            {
                public const string Success = "Resource has been successfully updated.";
                public const string SecuritySuccess = "Security settings have been successfully updated.";
                public const string ProfileSuccess = "Profile information has been successfully updated.";
            }
        }

        /// <summary>
        /// Active/Inactive status messages
        /// </summary>
        public static class Status
        {
            public static class User
            {
                public const string Activated = "User has been successfully activated.";
                public const string Deactivated = "User has been successfully deactivated.";
            }

            public static class Bank
            {
                public const string Activated = "Bank has been successfully activated.";
                public const string Deactivated = "Bank has been successfully deactivated.";
            }

            public static class Account
            {
                public const string Activated = "Account has been successfully activated.";
                public const string Deactivated = "Account has been successfully deactivated.";
            }

            public static class Currency
            {
                public const string Activated = "Currency has been successfully activated.";
                public const string Deactivated = "Currency has been successfully deactivated.";
            }

            public static class Generic
            {
                public const string Activated = "Resource has been successfully activated.";
                public const string Deactivated = "Resource has been successfully deactivated.";
            }
        }

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

        /// <summary>
        /// Role and permission messages
        /// </summary>
        public static class RolePermissions
        {
            public const string AssignmentSuccess = "Role permissions have been successfully assigned.";
            public const string UpdateSuccess = "User role has been successfully updated.";
        }

        /// <summary>
        /// General processing messages
        /// </summary>
        public static class Processing
        {
            public const string OperationSuccess = "Operation has been processed successfully.";
        }

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
        }
    }
}
