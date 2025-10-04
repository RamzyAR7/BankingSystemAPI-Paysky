using System.Collections.Generic;

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
            public const string CannotDeleteSelf = "Users cannot delete themselves.";
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
            public const string AccountOwnershipRequired = "You can only access accounts you own.";
            public const string InactiveAccountAccess = "Cannot access inactive account.";

            // Transaction Operations
            public const string CannotUseOthersAccounts = "You cannot use accounts you don't own for transactions.";
            public const string InsufficientFundsForOperation = "Insufficient funds to complete this operation.";
            public const string SameAccountTransferBlocked = "Cannot transfer to the same account.";

            // System & Unknown
            public const string UnknownAccessScope = "Unknown access scope.";
            public const string SystemError = "A system error occurred during authorization.";
            public const string ResourceNotFound = "The requested resource was not found.";
        }

        #endregion

        #region Validation Rules - Organized by Priority

        public static class ValidationRules
        {
            // Critical Security Rules (Priority 1)
            public static readonly Dictionary<string, string> CriticalRules = new()
            {
                ["SELF_DELETE_BLOCKED"] = ErrorMessages.CannotDeleteSelf,
                ["SELF_EDIT_BLOCKED"] = ErrorMessages.CannotModifySelf,
                ["AUTHENTICATION_REQUIRED"] = ErrorMessages.NotAuthenticated
            };

            // High Priority Business Rules (Priority 2) 
            public static readonly Dictionary<string, string> HighPriorityRules = new()
            {
                ["ADMIN_CLIENT_ONLY"] = ErrorMessages.AdminsViewClientsOnly,
                ["BANK_ISOLATION"] = ErrorMessages.BankIsolationPolicy,
                ["ACCOUNT_OWNERSHIP"] = ErrorMessages.AccountOwnershipRequired
            };

            // Medium Priority Rules (Priority 3)
            public static readonly Dictionary<string, string> MediumPriorityRules = new()
            {
                ["CLIENT_MODIFY_BLOCKED"] = ErrorMessages.ClientsModifyOthersBlocked,
                ["CLIENT_CREATE_BLOCKED"] = ErrorMessages.ClientsCreateUsersBlocked,
                ["INACTIVE_ACCOUNT"] = ErrorMessages.InactiveAccountAccess
            };

            // Low Priority Rules (Priority 4)
            public static readonly Dictionary<string, string> LowPriorityRules = new()
            {
                ["UNKNOWN_SCOPE"] = ErrorMessages.UnknownAccessScope,
                ["RESOURCE_NOT_FOUND"] = ErrorMessages.ResourceNotFound
            };
        }

        #endregion

        #region Authorization Scopes - Ordered by Restriction Level

        public static class ScopeHierarchy
        {
            public const int SelfLevel = 1;      // Most restrictive
            public const int BankLevel = 2;      // Moderately restrictive  
            public const int GlobalLevel = 3;    // Least restrictive

            public static readonly Dictionary<AccessScope, int> ScopeLevels = new()
            {
                [AccessScope.Self] = SelfLevel,
                [AccessScope.BankLevel] = BankLevel,
                [AccessScope.Global] = GlobalLevel
            };

            public static readonly Dictionary<AccessScope, string[]> ScopePermissions = new()
            {
                [AccessScope.Self] = new[] { "ReadSelf", "ChangeOwnPassword" },
                [AccessScope.BankLevel] = new[] { "ReadBankUsers", "ModifyClients", "CreateUsers" },
                [AccessScope.Global] = new[] { "ReadAllUsers", "ModifyAllUsers", "SystemAdministration" }
            };
        }

        #endregion

        #region Operation Types - Organized by Impact Level

        public static class OperationTypes
        {
            // Low Impact Operations
            public static readonly string[] ReadOperations = { "View", "Read", "List", "Search" };
            
            // Medium Impact Operations
            public static readonly string[] ModifyOperations = { "Edit", "Update", "ChangePassword" };
            
            // High Impact Operations
            public static readonly string[] CreateOperations = { "Create", "Add", "Register" };
            
            // Critical Impact Operations
            public static readonly string[] DeleteOperations = { "Delete", "Remove", "Deactivate" };
        }

        #endregion

        #region Resource Categories - Organized by Sensitivity

        public static class ResourceCategories
        {
            // Highly Sensitive Resources
            public static readonly string[] FinancialResources = { "Account", "Transaction", "Balance", "InterestLog" };
            
            // Moderately Sensitive Resources
            public static readonly string[] PersonalResources = { "User", "Profile", "Contact", "NationalId" };
            
            // Administrative Resources
            public static readonly string[] SystemResources = { "Role", "Permission", "Bank", "Currency" };
            
            // Public/Low Sensitivity Resources
            public static readonly string[] PublicResources = { "ExchangeRate", "InterestRate", "BankInfo" };
        }

        #endregion

        #region Role Hierarchies - Organized by Authority Level

        public static class RoleHierarchy
        {
            public const int ClientLevel = 1;        // Lowest authority
            public const int BankAdminLevel = 2;     // Medium authority
            public const int SuperAdminLevel = 3;    // Highest authority

            public static readonly Dictionary<string, int> RoleLevels = new()
            {
                ["Client"] = ClientLevel,
                ["Admin"] = BankAdminLevel,
                ["BankAdmin"] = BankAdminLevel,
                ["SuperAdmin"] = SuperAdminLevel,
                ["SystemAdmin"] = SuperAdminLevel
            };

            public static readonly Dictionary<string, AccessScope> RoleScopes = new()
            {
                ["Client"] = AccessScope.Self,
                ["Admin"] = AccessScope.BankLevel,
                ["BankAdmin"] = AccessScope.BankLevel,
                ["SuperAdmin"] = AccessScope.Global,
                ["SystemAdmin"] = AccessScope.Global
            };
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