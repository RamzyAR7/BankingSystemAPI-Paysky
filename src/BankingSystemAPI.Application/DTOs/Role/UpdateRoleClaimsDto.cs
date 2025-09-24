using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Role
{
    public class UpdateRoleClaimsDto
    {
        public string RoleName { get; set; }
        public List<string> Claims { get; set; }
    }
}
