using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Role request data transfer object for creating roles
    /// </summary>
    public class RoleReqDto
    {
        /// <summary>
        /// Role name
        /// </summary>
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
