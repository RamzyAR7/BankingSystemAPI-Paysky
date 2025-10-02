using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AutoMapper;
using BankingSystemAPI.Application.Mapping;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Application.Interfaces;
using Moq;

namespace BankingSystemAPI.UnitTests.Application.TransactionHandlers
{
    /// <summary>
    /// Integration tests for transaction handlers with different account types
    /// </summary>
    public class WithdrawCommandHandlerIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UnitOfWork _unitOfWork;

        public WithdrawCommandHandlerIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Create a proper mapper configuration for testing
            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
                .Returns((Transaction t) => new TransactionResDto 
                { 
                    TransactionId = t.Id, 
                    Timestamp = t.Timestamp,
                    TransactionType = t.TransactionType.ToString(),
                    Amount = t.AccountTransactions?.FirstOrDefault()?.Amount ?? 0m,
                    SourceAccountId = t.AccountTransactions?.FirstOrDefault()?.AccountId,
                    SourceCurrency = t.AccountTransactions?.FirstOrDefault()?.TransactionCurrency ?? "USD",
                    Fees = t.AccountTransactions?.FirstOrDefault()?.Fees ?? 0m
                });
            _mapper = mockMapper.Object;

            // Create mock cache service for repositories that need it
            var mockCache = new Mock<ICacheService>();

            // Setup repositories
            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, mockCache.Object);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTransactionRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var currencyRepo = new CurrencyRepository(_context, mockCache.Object);
            var bankRepo = new BankRepository(_context);

            _unitOfWork = new UnitOfWork(
                userRepo, roleRepo, accountRepo, transactionRepo, 
                accountTransactionRepo, interestLogRepo, currencyRepo, bankRepo, _context);
        }

        [Fact]
        public async Task WithdrawHandler_ShouldWork_WithSavingsAccount()
        {
            // Arrange
            var currency = new Currency 
            { 
                Id = 1, 
                Code = "USD", 
                IsBase = true, 
                ExchangeRate = 1.0m,
                IsActive = true 
            };
            
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                UserName = "testuser", 
                Email = "test@example.com",
                FullName = "Test User",
                NationalId = "1234567890",
                PhoneNumber = "+1234567890",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                IsActive = true 
            };
            
            var savingsAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "SAV123456789",
                Balance = 1000m,
                UserId = user.Id,
                User = user, // Set navigation property
                CurrencyId = currency.Id,
                Currency = currency, // Set navigation property
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Currencies.Add(currency);
            _context.Users.Add(user);
            _context.SavingsAccounts.Add(savingsAccount);
            await _context.SaveChangesAsync();

            var handler = new WithdrawCommandHandler(_unitOfWork, _mapper);
            var command = new WithdrawCommand(new WithdrawReqDto { AccountId = 1, Amount = 200m });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            
            // Verify account balance updated
            var updatedAccount = await _context.SavingsAccounts.FindAsync(1);
            Assert.NotNull(updatedAccount);
            Assert.Equal(800m, updatedAccount.Balance);
        }

        [Fact]
        public async Task WithdrawHandler_ShouldWork_WithCheckingAccount()
        {
            // Arrange
            var currency = new Currency 
            { 
                Id = 1, 
                Code = "USD", 
                IsBase = true, 
                ExchangeRate = 1.0m,
                IsActive = true 
            };
            
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                UserName = "testuser", 
                Email = "test@example.com",
                FullName = "Test User",
                NationalId = "1234567890",
                PhoneNumber = "+1234567890",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                IsActive = true 
            };
            
            var checkingAccount = new CheckingAccount
            {
                Id = 1,
                AccountNumber = "CHK123456789",
                Balance = 500m,
                UserId = user.Id,
                User = user, // Set navigation property
                CurrencyId = currency.Id,
                Currency = currency, // Set navigation property
                OverdraftLimit = 1000m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Currencies.Add(currency);
            _context.Users.Add(user);
            _context.CheckingAccounts.Add(checkingAccount);
            await _context.SaveChangesAsync();

            var handler = new WithdrawCommandHandler(_unitOfWork, _mapper);
            var command = new WithdrawCommand(new WithdrawReqDto { AccountId = 1, Amount = 800m });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            
            // Verify overdraft was used
            var updatedAccount = await _context.CheckingAccounts.FindAsync(1);
            Assert.NotNull(updatedAccount);
            Assert.Equal(-300m, updatedAccount.Balance);
            Assert.True(updatedAccount.IsOverdrawn());
        }

        [Fact]
        public async Task WithdrawHandler_ShouldFail_WithInsufficientFunds_SavingsAccount()
        {
            // Arrange
            var currency = new Currency 
            { 
                Id = 1, 
                Code = "USD", 
                IsBase = true, 
                ExchangeRate = 1.0m,
                IsActive = true 
            };
            
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                UserName = "testuser", 
                Email = "test@example.com",
                FullName = "Test User",
                NationalId = "1234567890",
                PhoneNumber = "+1234567890",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                IsActive = true 
            };
            
            var savingsAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "SAV123456789",
                Balance = 100m, // Low balance
                UserId = user.Id,
                User = user, // Set navigation property
                CurrencyId = currency.Id,
                Currency = currency, // Set navigation property
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Currencies.Add(currency);
            _context.Users.Add(user);
            _context.SavingsAccounts.Add(savingsAccount);
            await _context.SaveChangesAsync();

            var handler = new WithdrawCommandHandler(_unitOfWork, _mapper);
            var command = new WithdrawCommand(new WithdrawReqDto { AccountId = 1, Amount = 200m }); // More than balance

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Insufficient funds.", result.Errors); // Note the period at the end
        }

        [Fact]
        public async Task WithdrawHandler_ShouldFail_WithInvalidAmount()
        {
            // Arrange
            var currency = new Currency 
            { 
                Id = 1, 
                Code = "USD", 
                IsBase = true, 
                ExchangeRate = 1.0m,
                IsActive = true 
            };
            
            var user = new ApplicationUser 
            { 
                Id = "user1", 
                UserName = "testuser", 
                Email = "test@example.com",
                FullName = "Test User",
                NationalId = "1234567890",
                PhoneNumber = "+1234567890",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                IsActive = true 
            };
            
            var savingsAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "SAV123456789",
                Balance = 1000m,
                UserId = user.Id,
                User = user, // Set navigation property
                CurrencyId = currency.Id,
                Currency = currency, // Set navigation property
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Currencies.Add(currency);
            _context.Users.Add(user);
            _context.SavingsAccounts.Add(savingsAccount);
            await _context.SaveChangesAsync();

            var handler = new WithdrawCommandHandler(_unitOfWork, _mapper);
            var command = new WithdrawCommand(new WithdrawReqDto { AccountId = 1, Amount = -100m }); // Invalid negative amount

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Invalid amount.", result.Errors); // Note the period at the end
        }

        public void Dispose()
        {
            _unitOfWork?.Dispose();
            _context?.Dispose();
        }
    }
}