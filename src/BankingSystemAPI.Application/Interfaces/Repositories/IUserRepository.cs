using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<ApplicationUser, string>
    {
        /// <summary>
        /// Gets users by bank ID for validation purposes
        /// </summary>
        Task<IEnumerable<ApplicationUser>> GetUsersByBankIdAsync(int bankId);
        
        /// <summary>
        /// Query users by bank ID
        /// </summary>
        IQueryable<ApplicationUser> QueryByBankId(int bankId);
    }
}
