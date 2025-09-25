using System;
using System.Threading.Tasks;
using Xunit;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.UnitTests
{
    public class TransactionServiceInactiveAccountTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TransactionService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly Mock<ITransactionHelperService> _helperMock;

        public TransactionServiceInactiveAccountTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(_context);
            _userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            _currentUserMock = new Mock<ICurrentUserService>();

            var currencyRepo = new CurrencyRepository(_context);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>())).Returns(new TransactionResDto { TransactionId = 1 });
            _mapper = mapperMock.Object;

            _helperMock = new Mock<ITransactionHelperService>();

            _service = new TransactionService(_uow, _mapper, _helperMock.Object, _userManager, _currentUserMock.Object, new NullLogger<TransactionService>());
        }

        [Fact]
        public async Task Deposit_InactiveAccount_Throws()
        {
            var currency = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
            _context.Currencies.Add(currency);
            _context.SaveChanges();
            var user = new ApplicationUser { UserName = "txuser", Email = "txuser@example.com", PhoneNumber = "3000000001", FullName = "TX User", NationalId = "TXNID", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user, "Password123!");
            var account = new CheckingAccount { AccountNumber = "TXA1", Balance = 100m, UserId = user.Id, CurrencyId = currency.Id, IsActive = false, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(account);
            _context.SaveChanges();
            var req = new DepositReqDto { AccountId = account.Id, Amount = 10m };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => _service.DepositAsync(req));
        }

        [Fact]
        public async Task Withdraw_InactiveAccount_Throws()
        {
            var currency = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
            _context.Currencies.Add(currency);
            _context.SaveChanges();
            var user = new ApplicationUser { UserName = "txuser2", Email = "txuser2@example.com", PhoneNumber = "3000000002", FullName = "TX User2", NationalId = "TXNID2", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user, "Password123!");
            var account = new CheckingAccount { AccountNumber = "TXA2", Balance = 100m, UserId = user.Id, CurrencyId = currency.Id, IsActive = false, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(account);
            _context.SaveChanges();
            var req = new WithdrawReqDto { AccountId = account.Id, Amount = 10m };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => _service.WithdrawAsync(req));
        }

        [Fact]
        public async Task Transfer_InactiveSourceAccount_Throws()
        {
            var currency = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
            _context.Currencies.Add(currency);
            _context.SaveChanges();
            var user1 = new ApplicationUser { UserName = "txuser3", Email = "txuser3@example.com", PhoneNumber = "3000000003", FullName = "TX User3", NationalId = "TXNID3", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            var user2 = new ApplicationUser { UserName = "txuser4", Email = "txuser4@example.com", PhoneNumber = "3000000004", FullName = "TX User4", NationalId = "TXNID4", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user1, "Password123!");
            await _userManager.CreateAsync(user2, "Password123!");
            var srcAccount = new CheckingAccount { AccountNumber = "TXA3", Balance = 100m, UserId = user1.Id, CurrencyId = currency.Id, IsActive = false, RowVersion = new byte[8] };
            var tgtAccount = new CheckingAccount { AccountNumber = "TXA4", Balance = 100m, UserId = user2.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            _context.CheckingAccounts.AddRange(srcAccount, tgtAccount);
            _context.SaveChanges();
            var req = new TransferReqDto { SourceAccountId = srcAccount.Id, TargetAccountId = tgtAccount.Id, Amount = 10m };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => _service.TransferAsync(req));
        }

        [Fact]
        public async Task Transfer_InactiveTargetAccount_Throws()
        {
            var currency = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true, IsActive = true };
            _context.Currencies.Add(currency);
            _context.SaveChanges();
            var user1 = new ApplicationUser { UserName = "txuser5", Email = "txuser5@example.com", PhoneNumber = "3000000005", FullName = "TX User5", NationalId = "TXNID5", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            var user2 = new ApplicationUser { UserName = "txuser6", Email = "txuser6@example.com", PhoneNumber = "3000000006", FullName = "TX User6", NationalId = "TXNID6", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user1, "Password123!");
            await _userManager.CreateAsync(user2, "Password123!");
            var srcAccount = new CheckingAccount { AccountNumber = "TXA5", Balance = 100m, UserId = user1.Id, CurrencyId = currency.Id, IsActive = true, RowVersion = new byte[8] };
            var tgtAccount = new CheckingAccount { AccountNumber = "TXA6", Balance = 100m, UserId = user2.Id, CurrencyId = currency.Id, IsActive = false, RowVersion = new byte[8] };
            _context.CheckingAccounts.AddRange(srcAccount, tgtAccount);
            _context.SaveChanges();
            var req = new TransferReqDto { SourceAccountId = srcAccount.Id, TargetAccountId = tgtAccount.Id, Amount = 10m };
            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => _service.TransferAsync(req));
        }
    }
}

