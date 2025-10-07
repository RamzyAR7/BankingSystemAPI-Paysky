#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.DTOs.User
{
    public class UpdateUserRolesDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

