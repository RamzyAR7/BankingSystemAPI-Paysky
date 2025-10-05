#region Usings
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Grouped role claims response DTO.
    /// </summary>
    public class RoleClaimsResDto
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
        /// Name of the role or controller group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Permissions/claims in the group.
        /// </summary>
        public List<string> Claims { get; set; } = new List<string>();
    }
}

