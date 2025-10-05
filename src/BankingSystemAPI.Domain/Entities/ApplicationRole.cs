#region Usings
using Microsoft.AspNetCore.Identity;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public List<ApplicationUser> Users { get; set; }
    }
}

