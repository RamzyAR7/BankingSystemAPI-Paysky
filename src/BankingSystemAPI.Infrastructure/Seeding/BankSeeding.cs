using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Seeding
{
    public static class BankSeeding
    {
        public static async Task SeedAsync(DbContext context)
        {
            // Prevent reseeding if banks already exist
            if (!await context.Set<Bank>().AnyAsync())
            {
                var banks = new[]
                {
                    new Bank { Name = "National Bank of Egypt (NBE)" },//1
                    new Bank { Name = "Banque Misr(BM)" },//2
                    new Bank { Name = "Commercial International Bank (CIB)" },//3
                    new Bank { Name = "AlexBank (A)" }//4
                };

                await context.Set<Bank>().AddRangeAsync(banks);
                await context.SaveChangesAsync();
            }
        }
    }
}
