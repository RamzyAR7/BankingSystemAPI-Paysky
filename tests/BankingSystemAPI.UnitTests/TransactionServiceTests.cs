using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Moq;
using AutoMapper;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.DTOs.Currency;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Exceptions;

namespace BankingSystemAPI.UnitTests
{
    public class TransactionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly TransactionService _service;
        private readonly Mock<ITransactionHelperService> _helperMock;
        private readonly IMapper _mapper;

        public TransactionServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // seed currencies
            var usd = new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var eur = new Currency { Code = "EUR", ExchangeRate = 0.8m, IsBase = false };
            _context.Currencies.AddRange(usd, eur);
            _context.SaveChanges();

            // seed users
            var user1 = new ApplicationUser { UserName = "suser", Email = "s@example.com", PhoneNumber = "1000000001", FullName = "S User", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            user1.Id = Guid.NewGuid().ToString();
            var user2 = new ApplicationUser { UserName = "tuser", Email = "t@example.com", PhoneNumber = "1000000002", FullName = "T User", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            user2.Id = Guid.NewGuid().ToString();
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            // create accounts with RowVersion
            var src = new CheckingAccount { AccountNumber = "SRC1", Balance = 200m, UserId = user1.Id, CurrencyId = usd.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            var tgtUsd = new CheckingAccount { AccountNumber = "TGT1", Balance = 50m, UserId = user2.Id, CurrencyId = usd.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            var tgtEur = new CheckingAccount { AccountNumber = "TGT2", Balance = 50m, UserId = user2.Id, CurrencyId = eur.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            _context.CheckingAccounts.AddRange(src, tgtUsd, tgtEur);
            _context.SaveChanges();

            var currencyRepo = new CurrencyRepository(_context);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            // Remove roleRelationRepo from UnitOfWork constructor
            _uow = new UnitOfWork(accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            // mapper mock
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns((Transaction t) => new TransactionResDto { TransactionId = t.Id });
            _mapper = mapperMock.Object;

            // helper mock
            _helperMock = new Mock<ITransactionHelperService>();
            _helperMock.Setup(h => h.ConvertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync((string from, string to, decimal amt) =>
                {
                    // simple fake conversion: if USD->EUR multiply by 0.8, EUR->USD divide by 0.8
                    if (string.Equals(from, "USD", StringComparison.OrdinalIgnoreCase) && string.Equals(to, "EUR", StringComparison.OrdinalIgnoreCase))
                        return Math.Round(amt * 0.8m, 2);
                    if (string.Equals(from, "EUR", StringComparison.OrdinalIgnoreCase) && string.Equals(to, "USD", StringComparison.OrdinalIgnoreCase))
                        return Math.Round(amt / 0.8m, 2);
                    return amt;
                });

            // setup UserManager (not used for these tests but required by ctor)
            var userStore = new UserStore<ApplicationUser>(_context);
            var userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            var currentUserMock = new Mock<ICurrentUserService>();

            // setup bank auth mock to be permissive
            var bankAuthMock = new Mock<IBankAuthorizationHelper>();
            bankAuthMock.Setup(b => b.IsSuperAdminAsync()).ReturnsAsync(true);
            bankAuthMock.Setup(b => b.IsClientAsync()).ReturnsAsync(false);
            bankAuthMock.Setup(b => b.EnsureCanAccessAccountAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            bankAuthMock.Setup(b => b.EnsureCanInitiateTransferAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
            bankAuthMock.Setup(b => b.EnsureCanAccessUserAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            _service = new TransactionService(_uow, _mapper, _helperMock.Object, userManager, currentUserMock.Object, bankAuthMock.Object, new NullLogger<TransactionService>());
        }

        [Fact]
        public async Task Deposit_Success_UpdatesBalanceAndCreatesTransaction()
        {
            var account = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var req = new DepositReqDto { AccountId = account.Id, Amount = 100m };

            var res = await _service.DepositAsync(req);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(300m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.TransactionId);
            Assert.NotNull(trx);
            Assert.Equal(TransactionType.Deposit, trx.TransactionType);
            Assert.Single(trx.AccountTransactions);
            Assert.Equal(100m, trx.AccountTransactions.First().Amount);
        }

        [Fact]
        public async Task Deposit_InvalidAmount_Throws()
        {
            var account = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var req = new DepositReqDto { AccountId = account.Id, Amount = 0m };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.DepositAsync(req));
        }

        [Fact]
        public async Task Withdraw_Success_Checking_AllowsOverdraft()
        {
            var account = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            // allow overdraft
            account.Balance = 0m;
            account.OverdraftLimit = 50m;
            _context.SaveChanges();

            var req = new WithdrawReqDto { AccountId = account.Id, Amount = 40m };
            var res = await _service.WithdrawAsync(req);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(-40m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.TransactionId);
            Assert.NotNull(trx);
            Assert.Equal(TransactionType.Withdraw, trx.TransactionType);
        }

        [Fact]
        public async Task Withdraw_Savings_InsufficientFunds_Throws()
        {
            // create a savings account with low balance
            var sv = new SavingsAccount { AccountNumber = "S1", Balance = 10m, UserId = _context.Users.First().Id, CurrencyId = _context.Currencies.First().Id, RowVersion = new byte[8] };
            _context.SavingsAccounts.Add(sv);
            _context.SaveChanges();

            var req = new WithdrawReqDto { AccountId = sv.Id, Amount = 20m };
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WithdrawAsync(req));
        }

        [Fact]
        public async Task Transfer_SameCurrency_AppliesFeeAndUpdatesBalances()
        {
            var source = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var target = _context.CheckingAccounts.First(a => a.AccountNumber == "TGT1");

            // ensure sufficient balance
            source.Balance = 200m;
            target.Balance = 50m;
            _context.SaveChanges();

            var req = new TransferReqDto { SourceAccountId = source.Id, TargetAccountId = target.Id, Amount = 100m };
            var res = await _service.TransferAsync(req);

            var updatedSource = await _uow.AccountRepository.GetByIdAsync(source.Id);
            var updatedTarget = await _uow.AccountRepository.GetByIdAsync(target.Id);

            // fee = 0.5% of 100 = 0.5
            Assert.Equal(99.5m, updatedSource.Balance);
            Assert.Equal(150m, updatedTarget.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.TransactionId);
            Assert.NotNull(trx);
            Assert.Equal(2, trx.AccountTransactions.Count);
        }

        [Fact]
        public async Task Transfer_CrossCurrency_ConvertsAndAppliesHigherFee()
        {
            var source = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var target = _context.CheckingAccounts.First(a => a.AccountNumber == "TGT2");

            source.Balance = 200m;
            target.Balance = 50m;
            _context.SaveChanges();

            var req = new TransferReqDto { SourceAccountId = source.Id, TargetAccountId = target.Id, Amount = 100m };
            var res = await _service.TransferAsync(req);

            var updatedSource = await _uow.AccountRepository.GetByIdAsync(source.Id);
            var updatedTarget = await _uow.AccountRepository.GetByIdAsync(target.Id);

            // conversion: 100 USD -> 80 EUR (helper), fee = 1% of 100 = 1
            Assert.Equal(99m, updatedSource.Balance); // 200 - 100 - 1
            Assert.Equal(130m, updatedTarget.Balance); // 50 + 80
        }

        [Fact]
        public async Task Transfer_InvalidAmount_Throws()
        {
            var source = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var target = _context.CheckingAccounts.First(a => a.AccountNumber == "TGT1");
            var req = new TransferReqDto { SourceAccountId = source.Id, TargetAccountId = target.Id, Amount = 0m };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.TransferAsync(req));
        }

        [Fact]
        public async Task Transfer_SameAccount_Throws()
        {
            var acc = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var req = new TransferReqDto { SourceAccountId = acc.Id, TargetAccountId = acc.Id, Amount = 10m };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.TransferAsync(req));
        }

        [Fact]
        public async Task Transfer_InsufficientFunds_Throws()
        {
            var source = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var target = _context.CheckingAccounts.First(a => a.AccountNumber == "TGT1");
            source.Balance = 10m; // insufficient for 100 + fee
            _context.SaveChanges();

            var req = new TransferReqDto { SourceAccountId = source.Id, TargetAccountId = target.Id, Amount = 100m };
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TransferAsync(req));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
