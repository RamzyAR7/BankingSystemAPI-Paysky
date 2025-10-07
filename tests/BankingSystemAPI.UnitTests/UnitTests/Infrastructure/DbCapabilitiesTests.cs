using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using BankingSystemAPI.Application.Interfaces.Infrastructure;
using BankingSystemAPI.Infrastructure.Setting;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.UnitTests.UnitTests.Infrastructure
{
    public class DbCapabilitiesTests
    {
        [Fact]
        public void WhenApplicationDbContextRegistered_SupportsEfCoreAsync_IsTrue()
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<IDbCapabilities, DbCapabilities>();

            var provider = services.BuildServiceProvider();
            var caps = provider.GetRequiredService<IDbCapabilities>();

            Assert.True(caps.SupportsEfCoreAsync);
        }

        [Fact]
        public void WhenNoDbRegistered_SupportsEfCoreAsync_IsFalse()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDbCapabilities, DbCapabilities>();

            var provider = services.BuildServiceProvider();
            var caps = provider.GetRequiredService<IDbCapabilities>();

            Assert.False(caps.SupportsEfCoreAsync);
        }
    }
}
