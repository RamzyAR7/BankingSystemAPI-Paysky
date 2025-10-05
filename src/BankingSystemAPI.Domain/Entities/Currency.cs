#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class Currency
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public bool IsBase { get; set; }
        public decimal ExchangeRate { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Account> Accounts { get; set; }
    }
}

