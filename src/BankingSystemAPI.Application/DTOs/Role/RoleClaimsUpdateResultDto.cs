using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Result DTO for role claims update operations
    /// </summary>
    public class RoleClaimsUpdateResultDto
    {
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