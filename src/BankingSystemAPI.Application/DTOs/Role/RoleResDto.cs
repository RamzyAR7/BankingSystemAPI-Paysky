using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Role response data transfer object.
    /// </summary>
    public class RoleResDto
    {
        /// <summary>
        /// Role identifier (string).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Role name.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// List of claims associated with the role.
        /// </summary>
        public List<string> Claims { get; set; } = new List<string>();
    }
}
