#region Usings
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Result DTO for role update operations (create, update, delete)
    /// </summary>
    public class RoleUpdateResultDto
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
        /// Operation that was performed (Create, Update, Delete)
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// The role data containing all role information
        /// </summary>
        public RoleResDto? Role { get; set; }
    }
}
