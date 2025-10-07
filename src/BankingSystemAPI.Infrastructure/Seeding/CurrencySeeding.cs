#region Usings
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Infrastructure.Seeding
{
    public static class CurrencySeeding
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public static async Task SeedAsync(DbContext context)
        {
            // Check if any currencies already exist to prevent re-seeding
            if (!await context.Set<Currency>().AnyAsync())
            {
                var currencies = new[]
                {
                    // Rates updated as of September 18, 2025.
                    new Currency { Code = "USD", ExchangeRate = 1.0m, IsBase = true },      // US Dollar (Base Currency)
                    new Currency { Code = "EUR", ExchangeRate = 0.85m, IsBase = false },    // Euro
                    new Currency { Code = "GBP", ExchangeRate = 0.74m, IsBase = false },    // British Pound
                    new Currency { Code = "EGP", ExchangeRate = 48.19m, IsBase = false },   // Egyptian Pound
                    new Currency { Code = "SAR", ExchangeRate = 3.75m, IsBase = false }     // Saudi Riyal
                };

                await context.Set<Currency>().AddRangeAsync(currencies);
                await context.SaveChangesAsync();
            }
        }
    }
}

