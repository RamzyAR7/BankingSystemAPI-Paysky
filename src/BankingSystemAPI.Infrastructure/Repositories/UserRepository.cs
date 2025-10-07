#region Usings
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser, string>, IUserRepository
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByBankIdAsync(int bankId)
        {
            return await Table
                .Where(u => u.BankId == bankId)
                .AsNoTracking()
                .ToListAsync();
        }

        public IQueryable<ApplicationUser> QueryByBankId(int bankId)
        {
            return Table.Where(u => u.BankId == bankId);
        }
    }
}

