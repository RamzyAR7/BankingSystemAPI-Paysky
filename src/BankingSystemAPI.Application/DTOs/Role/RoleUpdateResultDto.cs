using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.Role
{
    /// <summary>
    /// Result DTO for role update operations (create, update, delete)
    /// </summary>
    public class RoleUpdateResultDto
    {
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