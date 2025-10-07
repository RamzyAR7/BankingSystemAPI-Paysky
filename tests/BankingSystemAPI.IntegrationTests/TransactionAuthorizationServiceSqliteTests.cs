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
    public class TransactionAuthorizationServiceSqliteTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly UnitOfWork _uow;

        public TransactionAuthorizationServiceSqliteTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // repositories + unit of work
            var noOpCache = new TestNoOpCache();
            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, noOpCache);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTransactionRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var currencyRepo = new CurrencyRepository(_context, noOpCache);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTransactionRepo, interestLogRepo, currencyRepo, bankRepo, _context);
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

        [Fact]
        public async Task FilterTransactionsAsync_Sqlite_RelationalRun_BankLevel()
        {
            // Arrange
            var bank = new Bank { Name = "SqlBank" };
            _context.Banks.Add(bank);
            await _context.SaveChangesAsync();

            var role = new ApplicationRole { Name = "UserRole" };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser { Id = "suser", UserName = "s", BankId = bank.Id, Email = "s@example.com", FullName = "S User", NationalId = "NIDS", PhoneNumber = "01000000009", RoleId = role.Id };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var currency = new Currency { Code = "USD", IsBase = false, ExchangeRate = 1m, IsActive = true };
            _context.Currencies.Add(currency);
            await _context.SaveChangesAsync();

            var account = new CheckingAccount { UserId = user.Id, CurrencyId = currency.Id, Balance = 100m, RowVersion = new byte[8] };
            // Add account, transaction and accountTransaction in one SaveChanges so EF orders inserts correctly for SQLite
            _context.CheckingAccounts.Add(account);
            var tx = new Transaction { Timestamp = DateTime.UtcNow };
            _context.Transactions.Add(tx);
            var at = new AccountTransaction { Account = account, Transaction = tx };
            _context.AccountTransactions.Add(at);
            await _context.SaveChangesAsync();

            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.SetupGet(x => x.UserId).Returns("actor");
            currentUserMock.SetupGet(x => x.BankId).Returns(bank.Id);
            currentUserMock.Setup(x => x.GetRoleFromStoreAsync()).ReturnsAsync(new ApplicationRole { Name = "Admin" });

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

            var query = _uow.TransactionRepository.QueryWithAccountTransactions();
            var result = await txAuth.FilterTransactionsAsync(query, pageNumber: 1, pageSize: 10);

            Assert.True(result.IsSuccess);
            var (items, total) = result.Value;
            Assert.Equal(1, total);
            Assert.Single(items);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }
    }
}
