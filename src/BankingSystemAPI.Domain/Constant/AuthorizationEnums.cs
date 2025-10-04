using System;

namespace BankingSystemAPI.Domain.Constant
{
    /// <summary>
    /// Defines access scopes for authorization in hierarchical order from most restrictive to least restrictive
    /// </summary>
    public enum AccessScope
    {
        /// <summary>
        /// Most restrictive: Users can only access their own data
        /// Applied to: Clients
        /// </summary>
        Self = 1,

        /// <summary>
        /// Bank-level access: Users can access data within their bank with role restrictions
        /// Applied to: Bank Admins, Regional Managers
        /// </summary>
        BankLevel = 2,

        /// <summary>
        /// Least restrictive: Full system access
        /// Applied to: Super Admins, System Administrators
        /// </summary>
        Global = 3
    }

    /// <summary>
    /// Defines authorization validation results in order of severity
    /// </summary>
    public enum AuthorizationResult
    {
        /// <summary>
        /// Operation is explicitly forbidden due to business rules
        /// </summary>
        Forbidden = 1,

        /// <summary>
        /// User lacks proper authentication
        /// </summary>
        Unauthorized = 2,

        /// <summary>
        /// Requested resource does not exist or is inaccessible
        /// </summary>
        NotFound = 3,

        /// <summary>
        /// Operation violates data integrity or business constraints
        /// </summary>
        ValidationFailed = 4,

        /// <summary>
        /// Operation is allowed and can proceed
        /// </summary>
        Allowed = 5
    }

    /// <summary>
    /// Defines authorization check types in logical operation order
    /// </summary>
    public enum AuthorizationCheckType
    {
        /// <summary>
        /// Check if user can view/read resources
        /// </summary>
        View = 1,

        /// <summary>
        /// Check if user can create new resources
        /// </summary>
        Create = 2,

        /// <summary>
        /// Check if user can modify existing resources
        /// </summary>
        Modify = 3,

        /// <summary>
        /// Check if user can delete/remove resources
        /// </summary>
        Delete = 4,

        /// <summary>
        /// Check if user can perform administrative operations
        /// </summary>
        Administrate = 5
    }

    /// <summary>
    /// Defines resource access patterns in order of complexity
    /// </summary>
    public enum ResourceAccessPattern
    {
        /// <summary>
        /// Single resource access by ID
        /// </summary>
        SingleResource = 1,

        /// <summary>
        /// Multiple resources owned by same user
        /// </summary>
        UserOwnedResources = 2,

        /// <summary>
        /// Resources within same bank/organization
        /// </summary>
        BankScopedResources = 3,

        /// <summary>
        /// Cross-bank resource access
        /// </summary>
        CrossBankResources = 4,

        /// <summary>
        /// System-wide resource access
        /// </summary>
        SystemWideResources = 5
    }

    /// <summary>
    /// Defines validation priority levels for authorization checks
    /// </summary>
    public enum ValidationPriority
    {
        /// <summary>
        /// Critical security checks (authentication, basic permissions)
        /// </summary>
        Critical = 1,

        /// <summary>
        /// High priority checks (ownership, scope validation)
        /// </summary>
        High = 2,

        /// <summary>
        /// Medium priority checks (business rules, data integrity)
        /// </summary>
        Medium = 3,

        /// <summary>
        /// Low priority checks (logging, audit trail)
        /// </summary>
        Low = 4
    }
}
