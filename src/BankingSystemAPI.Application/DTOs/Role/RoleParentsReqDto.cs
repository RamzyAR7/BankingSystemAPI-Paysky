using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Role
{
    public class RoleParentsReqDto
    {
        public string? ChildRole { get; set; }
        public List<string>? ParentRoles { get; set; }
    }
}
