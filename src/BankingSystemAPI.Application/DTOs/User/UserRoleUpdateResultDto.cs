using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using BankingSystemAPI.Application.DTOs.User;

namespace BankingSystemAPI.Application.DTOs.User
{
    public class UserRoleUpdateResultDto
    {
        public bool Succeeded { get; set; }
        public List<IdentityError> Errors { get; set; } = new();
        public UserRoleResDto? UserRole { get; set; }
    }
}
