#region Usings
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Role request data transfer object for creating roles
    /// </summary>
    public class RoleReqDto
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
        /// Role name
        /// </summary>
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters")]
        public string Name { get; set; } = string.Empty;
    }
}

