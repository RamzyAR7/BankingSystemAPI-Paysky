using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using BankingSystemAPI.Application.DTOs.Role;

namespace BankingSystemAPI.Application.DTOs.Role
{
    public class RoleUpdateResultDto
    {
        public bool Succeeded { get; set; }
        public List<IdentityError> Errors { get; set; } = new();
        public RoleResDto? Role { get; set; }
    }
}
