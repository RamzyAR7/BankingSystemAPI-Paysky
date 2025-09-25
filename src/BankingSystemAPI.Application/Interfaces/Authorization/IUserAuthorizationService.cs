using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IUserAuthorizationService
    {
        Task CanViewUserAsync(string targetUserId);
        Task CanModifyUserAsync(string targetUserId, UserModificationOperation operation);
        Task<IEnumerable<ApplicationUser>> FilterUsersAsync(IEnumerable<ApplicationUser> users);
        Task CanCreateUserAsync();
    }
}
