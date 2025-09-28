using Microsoft.AspNetCore.Identity;

namespace BankingSystemAPI.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public List<ApplicationUser> Users { get; set; }
    }
}
