using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IUserRepository: IGenericRepository<ApplicationUser, string>
    {
        Task<IEnumerable<ApplicationUser>> GetUsersByIdsAsync(IEnumerable<string> ids, Expression<Func<ApplicationUser, object>>[] includeExpressions = null, bool asNoTracking = true);
    }
}
