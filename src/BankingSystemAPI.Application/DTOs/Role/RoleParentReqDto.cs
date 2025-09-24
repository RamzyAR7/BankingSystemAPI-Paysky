using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Role
{
    public class RoleParentReqDto
    {
        public string? ChildRole { get; set; }
        public string? ParentRole { get; set; }
    }
}
