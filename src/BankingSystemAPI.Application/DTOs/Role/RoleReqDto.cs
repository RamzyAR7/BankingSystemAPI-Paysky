using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Role
{
    public class RoleReqDto
    {
        public string Name { get; set; }
        public List<string>? ParentRoleNames { get; set; }
    }
}
