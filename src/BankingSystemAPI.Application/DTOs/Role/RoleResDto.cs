#region Usings
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Role response data transfer object.
    /// </summary>
    public class RoleResDto
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
        /// Role identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Role name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of claims associated with the role
        /// </summary>
        public List<string> Claims { get; set; } = new List<string>();
    }
}

