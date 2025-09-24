using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Grouped role claims response DTO.
    /// </summary>
    public class RoleClaimsResDto
    {
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
