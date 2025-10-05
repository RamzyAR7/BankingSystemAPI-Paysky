#region Usings
using System;
#endregion


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
}

