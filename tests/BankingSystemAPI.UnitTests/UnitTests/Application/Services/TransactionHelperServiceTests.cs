using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.UnitTests.UnitTests.Application.Services;

public class TransactionHelperServiceTests : TestBase
{
    protected override void ConfigureMapperMock(Mock<AutoMapper.IMapper> mapperMock)
    {
        // No mapping required
    }

    [Fact]
    public async Task ConvertAsync_SameCurrencyById_ReturnsAmount()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TransactionHelperService>();
        var service = new TransactionHelperService(UnitOfWork, logger, cache);

        var amount = await service.ConvertAsync(1, 1, 100m);
        Assert.Equal(100m, amount);
    }

    [Fact]
    public async Task ConvertAsync_ByCode_PerformsConversion()
    {
        // Arrange: create two currencies
        var from = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
        var to = new Currency { Code = "EUR", ExchangeRate = 0.9m, IsBase = false, IsActive = true };
        Context.Currencies.Add(from);
        Context.Currencies.Add(to);
        Context.SaveChanges();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TransactionHelperService>();
        var service = new TransactionHelperService(UnitOfWork, logger, cache);

        var converted = await service.ConvertAsync("USD", "EUR", 100m);
        // USD base -> EUR rate 0.9 -> 100 * 0.9 = 90
        Assert.Equal(90m, converted);
    }

    [Fact]
    public async Task ConvertAsync_InvalidAmount_Throws()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TransactionHelperService>();
        var service = new TransactionHelperService(UnitOfWork, logger, cache);

        await Assert.ThrowsAsync<System.InvalidOperationException>(() => service.ConvertAsync(1, 2, 0m));
    }
}
