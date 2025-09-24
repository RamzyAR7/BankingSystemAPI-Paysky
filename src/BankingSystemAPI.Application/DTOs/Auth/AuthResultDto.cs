using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.DTOs.Auth
{
    public class AuthResultDto
    {
        public bool Succeeded { get; set; }
        public List<IdentityError> Errors { get; set; } = new();
        public AuthResDto? AuthData { get; set; }
        public string? Message { get; set; }
    }
}
