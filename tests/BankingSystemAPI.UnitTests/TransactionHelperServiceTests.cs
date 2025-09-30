using System;
using System.Threading.Tasks;
using Xunit;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Exceptions;
using Moq;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;

namespace BankingSystemAPI.UnitTests
{
    public class TransactionHelperServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TransactionHelperService _service;
        private readonly IUnitOfWork _uow;

        public TransactionHelperServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Services.MemoryCacheService(memoryCache);

            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);
            _service = new TransactionHelperService(_uow);

            // seed currencies
            _context.Currencies.Add(new Domain.Entities.Currency { Code = "BASE", ExchangeRate = 1m, IsBase = true });
            _context.Currencies.Add(new Domain.Entities.Currency { Code = "A", ExchangeRate = 2m, IsBase = false });
            _context.Currencies.Add(new Domain.Entities.Currency { Code = "B", ExchangeRate = 0.5m, IsBase = false });
            _context.SaveChanges();
        }

        [Fact]
        public async Task Convert_ById_BaseToOther()
        {
            var baseCurr = await _uow.CurrencyRepository.FindAsync(new CurrencyByCodeSpecification("BASE"));
            var other = await _uow.CurrencyRepository.FindAsync(new CurrencyByCodeSpecification("A"));

            var converted = await _service.ConvertAsync(baseCurr.Id, other.Id, 10m);
            Assert.Equal(20m, converted);
        }

        [Fact]
        public async Task Convert_ByCode_OtherToBase()
        {
            var converted = await _service.ConvertAsync("A", "BASE", 20m);
            // A has rate 2 -> 20/2 =10
            Assert.Equal(10m, converted);
        }

        [Fact]
        public async Task Convert_SameCurrency_ReturnsAmount()
        {
            var currency = await _uow.CurrencyRepository.FindAsync(new CurrencyByCodeSpecification("A"));
            var converted = await _service.ConvertAsync(currency.Id, currency.Id, 5m);
            Assert.Equal(5m, converted);
        }

        [Fact]
        public async Task Convert_InvalidCurrency_Throws()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.ConvertAsync(999, 1, 1m));
        }

        [Fact]
        public async Task Convert_InvalidAmount_Throws()
        {
            var currency = await _uow.CurrencyRepository.FindAsync(new CurrencyByCodeSpecification("A"));
            await Assert.ThrowsAsync<BadRequestException>(() => _service.ConvertAsync(currency.Id, 1, 0m));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
