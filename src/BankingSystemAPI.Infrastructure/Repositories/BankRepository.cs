using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class BankRepository : GenericRepository<Bank, int>, IBankRepository
    {
        private readonly ApplicationDbContext _context;
        public BankRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Dictionary<int, string>> GetBankNamesByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids?.Where(i => i > 0).Distinct().ToList();
            if (idList == null || !idList.Any()) return new Dictionary<int, string>();

            return await _context.Banks
                .Where(b => idList.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name);
        }
    }
}
