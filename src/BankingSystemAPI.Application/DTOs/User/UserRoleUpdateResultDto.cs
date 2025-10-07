#region Usings
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using BankingSystemAPI.Application.DTOs.User;
#endregion


namespace BankingSystemAPI.Application.DTOs.User
{
    /// <summary>
    /// Result DTO for user role update operations
    /// </summary>
    public class UserRoleUpdateResultDto
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        /// <summary>
        /// User identifier
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Previous role name (if tracked)
        /// </summary>
        public string? PreviousRole { get; set; }

        /// <summary>
        /// New role name assigned to user
        /// </summary>
        public string? NewRole { get; set; }

        /// <summary>
        /// The user role data
        /// </summary>
        public UserRoleResDto? UserRole { get; set; }
    }
}

