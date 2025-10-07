using BankingSystemAPI.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BankingSystemAPI.Infrastructure.Setting
{
    public class DbCapabilities : IDbCapabilities
    {
        private readonly bool _supportsEfCoreAsync;

        public DbCapabilities(Microsoft.Extensions.Options.IOptions<BankingSystemAPI.Application.Interfaces.Infrastructure.DbCapabilitiesOptions>? options = null, IServiceProvider? services = null)
        {
            var opt = options?.Value?.SupportsEfCoreAsync;
            if (opt.HasValue)
            {
                _supportsEfCoreAsync = opt.Value;
                return;
            }

            try
            {
                // If ApplicationDbContext or DbContextOptions<ApplicationDbContext> are registered, assume EF Core is available
                if (services != null)
                {
                    var appCtx = services.GetService(typeof(BankingSystemAPI.Infrastructure.Context.ApplicationDbContext));
                    var specificOptions = services.GetService(typeof(Microsoft.EntityFrameworkCore.DbContextOptions<BankingSystemAPI.Infrastructure.Context.ApplicationDbContext>));
                    _supportsEfCoreAsync = appCtx != null || specificOptions != null;
                }
                else
                {
                    _supportsEfCoreAsync = true;
                }
            }
            catch
            {
                // Conservative default
                _supportsEfCoreAsync = true;
            }
        }

        public bool SupportsEfCoreAsync => _supportsEfCoreAsync;
    }
}
