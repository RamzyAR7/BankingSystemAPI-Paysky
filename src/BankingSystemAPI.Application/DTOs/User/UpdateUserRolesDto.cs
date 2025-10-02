using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.User
{
    public class UpdateUserRolesDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
}
