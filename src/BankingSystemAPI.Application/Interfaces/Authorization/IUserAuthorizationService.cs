using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IUserAuthorizationService
    {
        Task CanViewUserAsync(string targetUserId);
        Task CanModifyUserAsync(string targetUserId, UserModificationOperation operation);
        Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> FilterUsersAsync(
            IQueryable<ApplicationUser> query,
            int pageNumber = 1,
            int pageSize = 10);
        Task CanCreateUserAsync();
    }
}
