#region Usings
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Result DTO for role claims update operations
    /// </summary>
    public class RoleClaimsUpdateResultDto
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
        /// Role name that was updated
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Updated claims for the role
        /// </summary>
        public List<string> UpdatedClaims { get; set; } = new List<string>();
    }
}
