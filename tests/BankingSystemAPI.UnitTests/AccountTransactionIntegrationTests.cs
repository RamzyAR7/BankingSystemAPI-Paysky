using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Services;

namespace BankingSystemAPI.UnitTests
{
    public class AccountTransactionIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITransactionHelperService> _helperMock;

        public AccountTransactionIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Cache.MemoryCacheService(memoryCache);

            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns((Transaction t) => new TransactionResDto { TransactionId = t.Id });

            _helperMock = new Mock<ITransactionHelperService>();
            // default convert simply returns same amount
            _helperMock.Setup(h => h.ConvertAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync((int f, int t, decimal amt) => amt);

            // seed a currency
            var curr = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
            _context.Currencies.Add(curr);
            _context.SaveChanges();
        }

        [Fact]
        public async Task Deposit_CreatesTransactionAndUpdatesBalance()
        {
            // arrange
            var user = new ApplicationUser
            {
                Id = "u1",
                UserName = "u1",
                Email = "u1@example.com",
                PhoneNumber = "0000000001",
                FullName = "User One",
                NationalId = Guid.NewGuid().ToString().Substring(0, 10),
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                IsActive = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var currency = _context.Currencies.First();
            var account = new CheckingAccount { AccountNumber = "A1", Balance = 100m, UserId = user.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(account);
            _context.SaveChanges();

            var depositHandler = new DepositCommandHandler(_uow, _mapperMock.Object);
            var req = new DepositReqDto { AccountId = account.Id, Amount = 25m };

            // act
            var res = await depositHandler.Handle(new DepositCommand(req), default);

            // assert
            Assert.True(res.Succeeded);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(125m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).ThenInclude(at => at.Account).FirstOrDefault();
            Assert.NotNull(trx);
            var atx = trx.AccountTransactions.FirstOrDefault();
            Assert.NotNull(atx);
            Assert.Equal(account.Id, atx.AccountId);
            Assert.Equal("USD", atx.TransactionCurrency);
            Assert.Equal(25m, atx.Amount);
            Assert.Equal(0m, atx.Fees);
        }

        [Fact]
        public async Task Withdraw_CreatesTransactionAndUpdatesBalance()
        {
            // arrange
            var user = new ApplicationUser
            {
                Id = "u2",
                UserName = "u2",
                Email = "u2@example.com",
                PhoneNumber = "0000000002",
                FullName = "User Two",
                NationalId = Guid.NewGuid().ToString().Substring(0, 10),
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                IsActive = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var currency = _context.Currencies.First();
            var account = new CheckingAccount { AccountNumber = "A2", Balance = 200m, UserId = user.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(account);
            _context.SaveChanges();

            var withdrawHandler = new WithdrawCommandHandler(_uow, _mapperMock.Object);
            var req = new WithdrawReqDto { AccountId = account.Id, Amount = 50m };

            // act
            var res = await withdrawHandler.Handle(new WithdrawCommand(req), default);

            // assert
            Assert.True(res.Succeeded);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(150m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).ThenInclude(at => at.Account).FirstOrDefault(t => t.AccountTransactions.Any(at=>at.AccountId==account.Id));
            Assert.NotNull(trx);
            var atx = trx.AccountTransactions.FirstOrDefault(at => at.AccountId == account.Id);
            Assert.NotNull(atx);
            Assert.Equal("USD", atx.TransactionCurrency);
            Assert.Equal(50m, atx.Amount);
            Assert.Equal(0m, atx.Fees);
        }

        [Fact]
        public async Task Transfer_CreatesTransactionAndUpdatesBothBalances()
        {
            // arrange
            var user = new ApplicationUser
            {
                Id = "u3",
                UserName = "u3",
                Email = "u3@example.com",
                PhoneNumber = "0000000003",
                FullName = "User Three",
                NationalId = Guid.NewGuid().ToString().Substring(0, 10),
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                IsActive = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var currency = _context.Currencies.First();
            var src = new CheckingAccount { AccountNumber = "S1", Balance = 300m, UserId = user.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            var tgt = new CheckingAccount { AccountNumber = "T1", Balance = 50m, UserId = user.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            _context.CheckingAccounts.AddRange(src, tgt);
            _context.SaveChanges();

            var transferHandler = new TransferCommandHandler(_uow, _mapperMock.Object, _helperMock.Object, null);
            var req = new TransferReqDto { SourceAccountId = src.Id, TargetAccountId = tgt.Id, Amount = 100m };

            // act
            var res = await transferHandler.Handle(new TransferCommand(req), default);

            // assert
            Assert.True(res.Succeeded);

            var updatedSrc = await _uow.AccountRepository.GetByIdAsync(src.Id);
            var updatedTgt = await _uow.AccountRepository.GetByIdAsync(tgt.Id);

            // fee for same currency is 0.5% of 100 = 0.5
            Assert.Equal(199.5m, updatedSrc.Balance);
            Assert.Equal(150m, updatedTgt.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).ThenInclude(at => at.Account).FirstOrDefault(t => t.TransactionType == BankingSystemAPI.Domain.Constant.TransactionType.Transfer);
            Assert.NotNull(trx);
            Assert.Equal(2, trx.AccountTransactions.Count);

            var srcAt = trx.AccountTransactions.First(at => at.Role == BankingSystemAPI.Domain.Constant.TransactionRole.Source);
            var tgtAt = trx.AccountTransactions.First(at => at.Role == BankingSystemAPI.Domain.Constant.TransactionRole.Target);
            Assert.Equal("USD", srcAt.TransactionCurrency);
            Assert.Equal("USD", tgtAt.TransactionCurrency);
            Assert.Equal(100m, srcAt.Amount);
            Assert.Equal(100m, tgtAt.Amount);
            Assert.Equal(0.5m, srcAt.Fees);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
