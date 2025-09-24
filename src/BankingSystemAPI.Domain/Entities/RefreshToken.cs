using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Entities
{
    public class RefreshToken
    {
        public string Token { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime AbsoluteExpiresOn { get; set; }
        public DateTime? RevokedOn { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Computed properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public bool IsAbsoluteExpired => DateTime.UtcNow >= AbsoluteExpiresOn;
        public bool IsActive => RevokedOn == null && !IsExpired && !IsAbsoluteExpired;
    }
}
