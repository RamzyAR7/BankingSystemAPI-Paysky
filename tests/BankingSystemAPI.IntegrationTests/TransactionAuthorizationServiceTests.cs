using System;
using System.Linq;
using System.Threading.Tasks;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Infrastructure.Setting;
using BankingSystemAPI.Application.Interfaces.Infrastructure;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankingSystemAPI.IntegrationTests
{
    public class TransactionAuthorizationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitOfWork _uow;

        public TransactionAuthorizationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Create repositories and unit of work
            var userRepo = new UserRepository(_context);
            // simple no-op cache for tests
            var noOpCache = new TestNoOpCache();
            var roleRepo = new RoleRepository(_context, noOpCache);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTransactionRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var currencyRepo = new CurrencyRepository(_context, noOpCache);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTransactionRepo, interestLogRepo, currencyRepo, bankRepo, _context);
        }

        [Fact]
        public async Task FilterTransactionsAsync_BankLevel_FiltersByBank()
        {
            // Arrange: two banks, userA in bank1, userB in bank2
            var bank1 = new Bank { Name = "Bank1" };
            var bank2 = new Bank { Name = "Bank2" };
            _context.Banks.AddRange(bank1, bank2);
            await _context.SaveChangesAsync();

            var userA = new ApplicationUser { Id = "userA", UserName = "a", BankId = bank1.Id, Email = "a@example.com", FullName = "User A", NationalId = "NIDA", PhoneNumber = "01000000001" };
            var userB = new ApplicationUser { Id = "userB", UserName = "b", BankId = bank2.Id, Email = "b@example.com", FullName = "User B", NationalId = "NIDB", PhoneNumber = "01000000002" };
            _context.Users.AddRange(userA, userB);
            await _context.SaveChangesAsync();

            var currency = new Currency { Code = "USD", IsBase = false, ExchangeRate = 1m, IsActive = true };
            _context.Currencies.Add(currency);
            await _context.SaveChangesAsync();

            var accountA = new CheckingAccount { UserId = userA.Id, CurrencyId = currency.Id, Balance = 100m };
            var accountB = new CheckingAccount { UserId = userB.Id, CurrencyId = currency.Id, Balance = 50m };
            _context.CheckingAccounts.AddRange(accountA, accountB);
            await _context.SaveChangesAsync();

            // Transaction involving accountA
            var tx1 = new Transaction { Timestamp = DateTime.UtcNow }; _context.Transactions.Add(tx1);
            await _context.SaveChangesAsync();
            var at1 = new AccountTransaction { AccountId = accountA.Id, TransactionId = tx1.Id };
            _context.AccountTransactions.Add(at1);

            // Transaction involving accountB
            var tx2 = new Transaction { Timestamp = DateTime.UtcNow.AddMinutes(-5) }; _context.Transactions.Add(tx2);
            await _context.SaveChangesAsync();
            var at2 = new AccountTransaction { AccountId = accountB.Id, TransactionId = tx2.Id };
            _context.AccountTransactions.Add(at2);

            await _context.SaveChangesAsync();

            // Current user context: acts as admin for bank1
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.SetupGet(x => x.UserId).Returns("actor");
            currentUserMock.SetupGet(x => x.BankId).Returns(bank1.Id);
            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "Admin" });

            // Scope resolver returns BankLevel
            var scopeResolverMock = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IScopeResolver>();
            scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // DbCapabilities: pretend EF Core async supported
            var dbCapMock = new Mock<BankingSystemAPI.Application.Interfaces.Infrastructure.IDbCapabilities>();
            dbCapMock.SetupGet(x => x.SupportsEfCoreAsync).Returns(true);

            var txAuth = new TransactionAuthorizationService(
                currentUserMock.Object,
                _uow,
                scopeResolverMock.Object,
                new NullLogger<TransactionAuthorizationService>(),
                dbCapMock.Object);

            // Act: query from TransactionRepository.QueryWithAccountTransactions()
            var query = _uow.TransactionRepository.QueryWithAccountTransactions();
            var result = await txAuth.FilterTransactionsAsync(query, pageNumber: 1, pageSize: 10);

            // Assert: only tx1 (accountA) should be returned
            Assert.True(result.IsSuccess);
            var (items, total) = result.Value;
            Assert.Equal(1, total);
            Assert.Contains(items, t => t.Id == tx1.Id);
            Assert.DoesNotContain(items, t => t.Id == tx2.Id);
        }

        [Fact]
        public async Task FilterTransactionsAsync_BankLevel_NoResults_WhenDifferentBank()
        {
            // Arrange: two banks, userA in bank1, userB in bank2
            var bank1 = new Bank { Name = "Bank1" };
            var bank2 = new Bank { Name = "Bank2" };
            _context.Banks.AddRange(bank1, bank2);
            await _context.SaveChangesAsync();

            var userA = new ApplicationUser { Id = "userA", UserName = "a", BankId = bank1.Id, Email = "a@example.com", FullName = "User A", NationalId = "NIDA", PhoneNumber = "01000000001" };
            var userB = new ApplicationUser { Id = "userB", UserName = "b", BankId = bank2.Id, Email = "b@example.com", FullName = "User B", NationalId = "NIDB", PhoneNumber = "01000000002" };
            _context.Users.AddRange(userA, userB);
            await _context.SaveChangesAsync();

            var currency = new Currency { Code = "USD", IsBase = false, ExchangeRate = 1m, IsActive = true };
            _context.Currencies.Add(currency);
            await _context.SaveChangesAsync();

            var accountA = new CheckingAccount { UserId = userA.Id, CurrencyId = currency.Id, Balance = 100m };
            var accountB = new CheckingAccount { UserId = userB.Id, CurrencyId = currency.Id, Balance = 50m };
            _context.CheckingAccounts.AddRange(accountA, accountB);
            await _context.SaveChangesAsync();

            // Transaction involving accountA
            var tx1 = new Transaction { Timestamp = DateTime.UtcNow }; _context.Transactions.Add(tx1);
            await _context.SaveChangesAsync();
            var at1 = new AccountTransaction { AccountId = accountA.Id, TransactionId = tx1.Id };
            _context.AccountTransactions.Add(at1);

            await _context.SaveChangesAsync();

            // Current user context: actor belongs to bank2 (different from accountA)
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.SetupGet(x => x.UserId).Returns("actor");
            currentUserMock.SetupGet(x => x.BankId).Returns(bank2.Id);
            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "Admin" });

            // Scope resolver returns BankLevel
            var scopeResolverMock = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IScopeResolver>();
            scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // DbCapabilities: pretend EF Core async supported
            var dbCapMock = new Mock<BankingSystemAPI.Application.Interfaces.Infrastructure.IDbCapabilities>();
            dbCapMock.SetupGet(x => x.SupportsEfCoreAsync).Returns(true);

            var txAuth = new TransactionAuthorizationService(
                currentUserMock.Object,
                _uow,
                scopeResolverMock.Object,
                new NullLogger<TransactionAuthorizationService>(),
                dbCapMock.Object);

            // Act: query from TransactionRepository.QueryWithAccountTransactions()
            var query = _uow.TransactionRepository.QueryWithAccountTransactions();
            var result = await txAuth.FilterTransactionsAsync(query, pageNumber: 1, pageSize: 10);

            // Assert: no transactions should be returned for bank2 actor
            Assert.True(result.IsSuccess);
            var (items, total) = result.Value;
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task FilterTransactionsAsync_Pagination_Works()
        {
            // Arrange: single bank and many transactions
            var bank = new Bank { Name = "BankPg" };
            _context.Banks.Add(bank);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser { Id = "user1", UserName = "u1", BankId = bank.Id, Email = "u1@example.com", FullName = "User 1", NationalId = "NID1", PhoneNumber = "01000000003" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var currency = new Currency { Code = "USD", IsBase = false, ExchangeRate = 1m, IsActive = true };
            _context.Currencies.Add(currency);
            await _context.SaveChangesAsync();

            var account = new CheckingAccount { UserId = user.Id, CurrencyId = currency.Id, Balance = 100m };
            _context.CheckingAccounts.Add(account);
            await _context.SaveChangesAsync();

            // Create 25 transactions for this account
            for (int i = 0; i < 25; i++)
            {
                var tx = new Transaction { Timestamp = DateTime.UtcNow.AddMinutes(-i) };
                _context.Transactions.Add(tx);
                await _context.SaveChangesAsync();
                var at = new AccountTransaction { AccountId = account.Id, TransactionId = tx.Id };
                _context.AccountTransactions.Add(at);
                await _context.SaveChangesAsync();
            }

            // Current user context: belongs to same bank
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.SetupGet(x => x.UserId).Returns("actor");
            currentUserMock.SetupGet(x => x.BankId).Returns(bank.Id);
            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "Admin" });

            // Scope resolver returns BankLevel
            var scopeResolverMock = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IScopeResolver>();
            scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            var dbCapMock = new Mock<BankingSystemAPI.Application.Interfaces.Infrastructure.IDbCapabilities>();
            dbCapMock.SetupGet(x => x.SupportsEfCoreAsync).Returns(true);

            var txAuth = new TransactionAuthorizationService(
                currentUserMock.Object,
                _uow,
                scopeResolverMock.Object,
                new NullLogger<TransactionAuthorizationService>(),
                dbCapMock.Object);

            // Act: request page 2 with pageSize 10 (should return items 11-20)
            var query = _uow.TransactionRepository.QueryWithAccountTransactions();
            var result = await txAuth.FilterTransactionsAsync(query, pageNumber: 2, pageSize: 10);

            // Assert
            Assert.True(result.IsSuccess);
            var (itemsPg2, totalCount) = result.Value;
            Assert.Equal(25, totalCount);
            Assert.Equal(10, itemsPg2.Count());
        }

        private class TestNoOpCache : BankingSystemAPI.Application.Interfaces.ICacheService
        {
            public bool TryGetValue<T>(object key, out T value)
            {
                value = default!;
                return false;
            }

            public void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null) { }
            public void Remove(object key) { }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
