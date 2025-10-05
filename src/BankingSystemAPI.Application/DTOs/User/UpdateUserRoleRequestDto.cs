#region Usings
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.User
{
    /// <summary>
    /// DTO for updating user role via REST endpoint
    /// </summary>
    public class UpdateUserRoleRequestDto
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
        /// The role name to assign to the user
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string Role { get; set; } = string.Empty;
    }
}
