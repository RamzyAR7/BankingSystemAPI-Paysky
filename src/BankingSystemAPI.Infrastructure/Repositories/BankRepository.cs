using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class BankRepository : GenericRepository<Bank, int>, IBankRepository
    {
        private readonly ApplicationDbContext _context;
        public BankRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> HasUsersAsync(int bankId)
        {
            return await _context.Users.AnyAsync(u => u.BankId == bankId);
        }
    }
}
