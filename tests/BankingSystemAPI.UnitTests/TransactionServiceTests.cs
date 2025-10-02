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
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Common;
using System.Linq;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;

namespace BankingSystemAPI.UnitTests
{
    public class TransactionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        // use handlers rather than a concrete service
        private readonly DepositCommandHandler _depositHandler;
        private readonly WithdrawCommandHandler _withdrawHandler;
        private readonly TransferCommandHandler _transferHandler;
        private readonly Mock<ITransactionHelperService> _helperMock;
        private readonly IMapper _mapper;
        private readonly Mock<IAccountAuthorizationService> _accountAuthMock;
        private readonly Mock<ITransactionAuthorizationService> _transactionAuthMock;

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
            var user1 = new ApplicationUser { UserName = "suser", Email = "s@example.com", PhoneNumber = "1000000001", FullName = "S User", NationalId = System.Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = System.DateTime.UtcNow.AddYears(-30) };
            user1.Id = System.Guid.NewGuid().ToString();
            var user2 = new ApplicationUser { UserName = "tuser", Email = "t@example.com", PhoneNumber = "1000000002", FullName = "T User", NationalId = System.Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = System.DateTime.UtcNow.AddYears(-30) };
            user2.Id = System.Guid.NewGuid().ToString();
            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();

            // create accounts with RowVersion
            var src = new CheckingAccount { AccountNumber = "SRC1", Balance = 200m, UserId = user1.Id, CurrencyId = usd.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            var tgtUsd = new CheckingAccount { AccountNumber = "TGT1", Balance = 50m, UserId = user2.Id, CurrencyId = usd.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            var tgtEur = new CheckingAccount { AccountNumber = "TGT2", Balance = 50m, UserId = user2.Id, CurrencyId = eur.Id, OverdraftLimit = 0m, RowVersion = new byte[8] };
            _context.CheckingAccounts.AddRange(src, tgtUsd, tgtEur);
            _context.SaveChanges();

            // create cache service and repositories with explicit DI (avoid legacy UnitOfWork ctor)
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

            // mapper mock
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns((Transaction t) => new TransactionResDto { TransactionId = t.Id });
            _mapper = mapperMock.Object;

            // helper mock
            _helperMock = new Mock<ITransactionHelperService>();

            // Setup id-based conversion mock
            _helperMock.Setup(h => h.ConvertAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync((int fromId, int toId, decimal amt) =>
                {
                    // map ids to codes using in-memory context
                    var from = _context.Currencies.Find(fromId);
                    var to = _context.Currencies.Find(toId);
                    if (from == null || to == null) return amt;

                    if (string.Equals(from.Code, "USD", System.StringComparison.OrdinalIgnoreCase) && string.Equals(to.Code, "EUR", System.StringComparison.OrdinalIgnoreCase))
                        return System.Math.Round(amt * 0.8m, 2);
                    if (string.Equals(from.Code, "EUR", System.StringComparison.OrdinalIgnoreCase) && string.Equals(to.Code, "USD", System.StringComparison.OrdinalIgnoreCase))
                        return System.Math.Round(amt / 0.8m, 2);
                    return amt;
                });

            // keep code-based overload mock for compatibility
            _helperMock.Setup(h => h.ConvertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync((string from, string to, decimal amt) =>
                {
                    // simple fake conversion: if USD->EUR multiply by 0.8, EUR->USD divide by 0.8
                    if (string.Equals(from, "USD", System.StringComparison.OrdinalIgnoreCase) && string.Equals(to, "EUR", System.StringComparison.OrdinalIgnoreCase))
                        return System.Math.Round(amt * 0.8m, 2);
                    if (string.Equals(from, "EUR", System.StringComparison.OrdinalIgnoreCase) && string.Equals(to, "USD", System.StringComparison.OrdinalIgnoreCase))
                        return System.Math.Round(amt / 0.8m, 2);
                    return amt;
                });

            // Setup Result-based authorization service mocks
            _accountAuthMock = new Mock<IAccountAuthorizationService>();
            _accountAuthMock.Setup(a => a.CanViewAccountAsync(It.IsAny<int>())).ReturnsAsync(Result.Success());
            _accountAuthMock.Setup(a => a.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>())).ReturnsAsync(Result.Success());
            _accountAuthMock.Setup(a => a.FilterAccountsQueryAsync(It.IsAny<IQueryable<Account>>()))
                .ReturnsAsync((IQueryable<Account> q) => Result<IQueryable<Account>>.Success(q));
            _accountAuthMock.Setup(a => a.CanCreateAccountForUserAsync(It.IsAny<string>())).ReturnsAsync(Result.Success());

            _transactionAuthMock = new Mock<ITransactionAuthorizationService>();
            _transactionAuthMock.Setup(t => t.CanInitiateTransferAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Result.Success());
            _transactionAuthMock.Setup(t => t.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((IQueryable<Transaction> txs, int page, int size) => 
                    Result<(IEnumerable<Transaction> Transactions, int TotalCount)>.Success((txs.ToList(), txs.Count())));

            // setup UserManager (not used for these tests but required by some constructors)
            var userStore = new UserStore<ApplicationUser>(_context);
            var userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());
            var currentUserMock = new Mock<ICurrentUserService>();

            // initialize handlers
            _depositHandler = new DepositCommandHandler(_uow, _mapper);
            _withdrawHandler = new WithdrawCommandHandler(_uow, _mapper);
            _transferHandler = new TransferCommandHandler(_uow, _mapper, _helperMock.Object, _transactionAuthMock.Object);
        }

        [Fact]
        public async Task Deposit_Success_UpdatesBalanceAndCreatesTransaction()
        {
            var account = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var req = new DepositReqDto { AccountId = account.Id, Amount = 100m };

            var res = await _depositHandler.Handle(new DepositCommand(req), CancellationToken.None);
            Assert.True(res.Succeeded);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(300m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.Value.TransactionId);
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
            var res = await _depositHandler.Handle(new DepositCommand(req), CancellationToken.None);
            Assert.False(res.Succeeded);
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
            var res = await _withdrawHandler.Handle(new WithdrawCommand(req), CancellationToken.None);
            Assert.True(res.Succeeded);

            var updated = await _uow.AccountRepository.GetByIdAsync(account.Id);
            Assert.Equal(-40m, updated.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.Value.TransactionId);
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
            var res = await _withdrawHandler.Handle(new WithdrawCommand(req), CancellationToken.None);
            Assert.False(res.Succeeded);
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
            var res = await _transferHandler.Handle(new TransferCommand(req), CancellationToken.None);
            Assert.True(res.Succeeded);

            var updatedSource = await _uow.AccountRepository.GetByIdAsync(source.Id);
            var updatedTarget = await _uow.AccountRepository.GetByIdAsync(target.Id);

            // fee = 0.5% of 100 = 0.5
            Assert.Equal(99.5m, updatedSource.Balance);
            Assert.Equal(150m, updatedTarget.Balance);

            var trx = _context.Transactions.Include(t => t.AccountTransactions).FirstOrDefault(t => t.Id == res.Value.TransactionId);
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
            var res = await _transferHandler.Handle(new TransferCommand(req), CancellationToken.None);
            Assert.True(res.Succeeded);

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
            var res = await _transferHandler.Handle(new TransferCommand(req), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Transfer_SameAccount_Throws()
        {
            var acc = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var req = new TransferReqDto { SourceAccountId = acc.Id, TargetAccountId = acc.Id, Amount = 10m };
            var res = await _transferHandler.Handle(new TransferCommand(req), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Transfer_InsufficientFunds_Throws()
        {
            var source = _context.CheckingAccounts.First(a => a.AccountNumber == "SRC1");
            var target = _context.CheckingAccounts.First(a => a.AccountNumber == "TGT1");
            source.Balance = 10m; // insufficient for 100 + fee
            _context.SaveChanges();

            var req = new TransferReqDto { SourceAccountId = source.Id, TargetAccountId = target.Id, Amount = 100m };
            var res = await _transferHandler.Handle(new TransferCommand(req), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
