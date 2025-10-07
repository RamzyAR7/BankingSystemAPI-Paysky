#region Usings
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using AutoMapper;
using Moq;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
#endregion


namespace BankingSystemAPI.UnitTests.TestInfrastructure;

/// <summary>
/// Base class for all unit tests providing common setup and utilities.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly ApplicationDbContext Context;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IMapper Mapper;

    protected TestBase()
    {
        // Create unique in-memory database for each test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();

        // Setup dependencies
        var cacheService = CreateCacheService();
        UnitOfWork = CreateUnitOfWork(cacheService);
        Mapper = CreateMapper();

        // Seed common test data
        SeedCommonTestData();
    }

    private MemoryCacheService CreateCacheService()
    {
        var memoryCache = new MemoryCache(
            new MemoryCacheOptions());
        var cacheLogger = new NullLogger<MemoryCacheService>();
        return new MemoryCacheService(memoryCache, cacheLogger);
    }

    private IUnitOfWork CreateUnitOfWork(MemoryCacheService cacheService)
    {
        var userRepo = new UserRepository(Context);
        var roleRepo = new RoleRepository(Context, cacheService);
        var currencyRepo = new CurrencyRepository(Context, cacheService);
        var accountRepo = new AccountRepository(Context);
        var transactionRepo = new TransactionRepository(Context);
        var accountTxRepo = new AccountTransactionRepository(Context);
        var interestLogRepo = new InterestLogRepository(Context);
        var bankRepo = new BankRepository(Context);

        return new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo,
            accountTxRepo, interestLogRepo, currencyRepo, bankRepo, Context);
    }

    protected virtual IMapper CreateMapper()
    {
        var mapperMock = new Mock<IMapper>();
        ConfigureMapperMock(mapperMock);
        return mapperMock.Object;
    }

    protected abstract void ConfigureMapperMock(Mock<IMapper> mapperMock);

    private void SeedCommonTestData()
    {
        // Base currency - required for most tests
        var baseCurrency = new Currency
        {
            Code = "USD",
            ExchangeRate = 1m,
            IsBase = true,
            IsActive = true
        };
        Context.Currencies.Add(baseCurrency);
        Context.SaveChanges();
    }

    protected ApplicationUser CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = email,
            PhoneNumber = "1234567890",
            FullName = "Test User",
            NationalId = Guid.NewGuid().ToString()[..10],
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            IsActive = true
        };

        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    protected Bank CreateTestBank(string name = "Test Bank")
    {
        var bank = new Bank
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        Context.Banks.Add(bank);
        Context.SaveChanges();
        return bank;
    }

    protected Currency GetBaseCurrency()
    {
        return Context.Currencies.First(c => c.IsBase);
    }

    public virtual void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
