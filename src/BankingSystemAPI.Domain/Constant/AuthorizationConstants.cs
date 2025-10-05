#region Usings
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Centralized authorization constants organized by functional areas
    /// </summary>
    public static class AuthorizationConstants
    {
        #region Error Messages - Organized by Scenario

        public static class ErrorMessages
        {
            // Authentication & Basic Access
            public const string NotAuthenticated = "User is not authenticated.";
            public const string InvalidToken = "Invalid or expired authentication token.";
            public const string InsufficientPermissions = "Insufficient permissions for this operation.";

            // Self-Access Restrictions  
            public const string CannotDeleteSelf = "Users cannot delete their own accounts.";
            public const string CannotModifySelf = "Users cannot edit their own profile details (only password).";
            public const string PasswordChangeOnly = "You can only change your own password.";

            // Role-Based Access
            public const string AdminsViewClientsOnly = "You can only access Client users.";
            public const string ClientsModifyOthersBlocked = "Clients cannot modify other users.";
            public const string ClientsCreateUsersBlocked = "Clients cannot create users.";
            public const string OnlyClientsCanBeModified = "Only Client users can be modified.";

            // Bank Isolation
            public const string BankIsolationPolicy = "Access forbidden due to bank isolation policy.";
            public const string DifferentBankAccess = "Access to resources from different bank is forbidden.";
            public const string MustBelongToBank = "User must belong to a bank to access this resource.";

            // Account Operations
            public const string CannotModifyOwnAccount = "Users cannot modify their own accounts directly.";
            public const string CannotFreezeOrUnfreezeOwnAccount = "Users cannot freeze or unfreeze their own accounts.";
            public const string AccountOwnershipRequired = "You can only access accounts you own.";
            public const string InactiveAccountAccess = "Cannot access inactive account.";

            // Transaction Operations
            public const string CannotUseOthersAccounts = "You cannot use accounts you don't own for transactions.";
            public const string InsufficientFundsForOperation = "Insufficient funds to complete this operation.";
            public const string SameAccountTransferBlocked = "Cannot transfer to the same account.";

            // Account Creation
            public const string AccountCreationAllowedForClients = "Account creation is permitted only for users with the 'Client' role. Ensure the target user has the 'Client' role or contact an administrator.";

            // System & Unknown
            public const string UnknownAccessScope = "Unknown access scope.";
            public const string SystemError = "A system error occurred during authorization.";
            public const string ResourceNotFound = "The requested resource was not found.";

            // Role claims modification specific
            public const string CannotModifySuperAdminRoleClaims = "Cannot modify claims for SuperAdmin role.";
            public const string OnlySuperAdminCanModifyClientRoleClaims = "Only SuperAdmin can modify claims for Client role.";
        }

        #endregion

        #region Logging Categories - Organized by Importance

        public static class LoggingCategories
        {

            public const string CRITICAL_SECURITY = "[CRITICAL_SECURITY]";
            public const string ACCESS_DENIED = "[ACCESS_DENIED]";
            public const string ACCESS_GRANTED = "[ACCESS_GRANTED]";
            public const string AUTHORIZATION_CHECK = "[AUTHORIZATION]";
            public const string BUSINESS_RULE = "[BUSINESS_RULE]";
            public const string VALIDATION_ERROR = "[VALIDATION]";
            public const string SYSTEM_ERROR = "[SYSTEM_ERROR]";
        }

        #endregion
    }
}
