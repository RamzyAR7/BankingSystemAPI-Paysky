using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Moq;
using AutoMapper;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.DTOs.Account;
using Xunit;
using System.Linq;
using System;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount;

namespace BankingSystemAPI.UnitTests
{
    public class CheckingAccountServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly CreateCheckingAccountCommandHandler _createHandler;
        private readonly UpdateCheckingAccountCommandHandler _updateHandler;

        public CheckingAccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Cache.MemoryCacheService(memoryCache);

            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<CheckingAccount>(It.IsAny<CheckingAccountReqDto>()))
                .Returns((CheckingAccountReqDto req) => new CheckingAccount { UserId = req.UserId, CurrencyId = req.CurrencyId, Balance = req.InitialBalance, OverdraftLimit = req.OverdraftLimit });
            mapperMock.Setup(m => m.Map<CheckingAccountDto>(It.IsAny<CheckingAccount>()))
                .Returns((CheckingAccount a) => new CheckingAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, OverdraftLimit = a.OverdraftLimit });

            _mapper = mapperMock.Object;

            _createHandler = new CreateCheckingAccountCommandHandler(_uow, _mapper);
            _updateHandler = new UpdateCheckingAccountCommandHandler(_uow, _mapper);

            // seed data
            _context.Currencies.Add(new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true });
            _context.SaveChanges();

            // Create a simple test user directly in context
            var user = new ApplicationUser 
            { 
                UserName = "user1", 
                Email = "u1@example.com", 
                PhoneNumber = "20000000001", 
                FullName = "User One", 
                NationalId = Guid.NewGuid().ToString().Substring(0, 10), 
                DateOfBirth = DateTime.UtcNow.AddYears(-30), 
                IsActive = true,
                RoleId = string.Empty // No role for simplicity
            };
            user.Id = Guid.NewGuid().ToString();
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Simple test to verify the setup works
            Assert.NotNull(_context);
            Assert.NotNull(_uow);
            Assert.NotNull(_mapper);
            Assert.NotNull(_createHandler);
            Assert.NotNull(_updateHandler);
        }

        [Fact]
        public void Context_HasRequiredData()
        {
            // Verify test data is seeded correctly
            Assert.True(_context.Currencies.Any());
            Assert.True(_context.Users.Any());
            
            var currency = _context.Currencies.First();
            Assert.Equal("USD", currency.Code);
            
            var user = _context.Users.First();
            Assert.Equal("user1", user.UserName);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

