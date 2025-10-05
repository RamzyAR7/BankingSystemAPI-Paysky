#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class Bank
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ApplicationUser> ApplicationUsers { get; set; }
    }
}

