#region Usings
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string NationalId { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; } = true;

        // Foreign Key of Bank
        public int? BankId { get; set; }
        public Bank Bank { get; set; }
        // Foreign Key of Role
        public string RoleId { get; set; } = string.Empty;
        public ApplicationRole Role { get; set; }

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}

