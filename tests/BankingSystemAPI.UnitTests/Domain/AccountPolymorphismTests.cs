using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Xunit;

namespace BankingSystemAPI.UnitTests.Domain
{
    /// <summary>
    /// Simple tests to verify Account hierarchy polymorphism and LSP compliance
    /// These tests focus on core domain behavior without external dependencies
    /// </summary>
    public class AccountPolymorphismTests
    {
        [Fact]
        public void Account_Polymorphic_Deposit_ShouldWork_ForAllAccountTypes()
        {
            // Arrange - Create different account types
            var savingsAccount = new SavingsAccount 
            { 
                Balance = 1000m,
                AccountNumber = "SAV123456789",
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var checkingAccount = new CheckingAccount 
            { 
                Balance = 1000m,
                AccountNumber = "CHK123456789", 
                OverdraftLimit = 500m,
                UserId = "user2",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert - Polymorphic behavior
            Account[] accounts = { savingsAccount, checkingAccount };
            
            foreach (var account in accounts)
            {
                var initialBalance = account.Balance;
                
                // Should work polymorphically without knowing specific type
                account.Deposit(100m);
                
                Assert.Equal(initialBalance + 100m, account.Balance);
            }
        }

        [Fact]
        public void Account_Polymorphic_GetAvailableBalance_ShouldRespectAccountRules()
        {
            // Arrange
            var savingsAccount = new SavingsAccount 
            { 
                Balance = 500m,
                AccountNumber = "SAV123456789",
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var checkingAccount = new CheckingAccount 
            { 
                Balance = 500m,
                AccountNumber = "CHK123456789",
                OverdraftLimit = 200m,
                UserId = "user2",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert - Different behavior but same interface
            Assert.Equal(500m, savingsAccount.GetAvailableBalance()); // Only balance
            Assert.Equal(700m, checkingAccount.GetAvailableBalance()); // Balance + overdraft
        }

        [Fact]
        public void SavingsAccount_ShouldNotAllowOverdraft()
        {
            // Arrange
            var savingsAccount = new SavingsAccount 
            { 
                Balance = 100m,
                AccountNumber = "SAV123456789",
                InterestRate = 0.05m,
                InterestType = InterestType.Monthly,
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Equal(100m, savingsAccount.GetAvailableBalance());
            
            // Should throw exception for insufficient funds
            Assert.Throws<InvalidOperationException>(() => 
                savingsAccount.Withdraw(150m));
                
            // Balance should remain unchanged
            Assert.Equal(100m, savingsAccount.Balance);
        }

        [Fact]
        public void CheckingAccount_ShouldAllowOverdraft()
        {
            // Arrange
            var checkingAccount = new CheckingAccount 
            { 
                Balance = 100m,
                OverdraftLimit = 200m,
                AccountNumber = "CHK123456789",
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Equal(300m, checkingAccount.GetAvailableBalance());
            
            // Should allow withdrawal beyond balance
            checkingAccount.Withdraw(150m);
            Assert.Equal(-50m, checkingAccount.Balance);
            
            // Verify overdraft methods
            Assert.True(checkingAccount.IsOverdrawn());
            Assert.Equal(50m, checkingAccount.GetOverdraftUsed());
            Assert.Equal(150m, checkingAccount.GetRemainingOverdraft());
        }

        [Fact]
        public void Account_ShouldMaintainConsistentExceptionBehavior()
        {
            // Arrange
            var accounts = new Account[]
            {
                new SavingsAccount 
                { 
                    Balance = 100m,
                    AccountNumber = "SAV123456789",
                    InterestRate = 0.05m,
                    InterestType = InterestType.Monthly,
                    UserId = "user1",
                    CurrencyId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new CheckingAccount 
                { 
                    Balance = 100m,
                    OverdraftLimit = 50m,
                    AccountNumber = "CHK123456789",
                    UserId = "user2",
                    CurrencyId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            // Act & Assert - All accounts should behave consistently for invalid operations
            foreach (var account in accounts)
            {
                // Should throw for negative amounts
                Assert.Throws<InvalidOperationException>(() => account.Withdraw(-10m));
                Assert.Throws<InvalidOperationException>(() => account.Withdraw(0m));
                Assert.Throws<InvalidOperationException>(() => account.Deposit(-10m));
                Assert.Throws<InvalidOperationException>(() => account.Deposit(0m));
            }
        }

        [Fact]
        public void Account_CanPerformTransactions_ShouldWork_Polymorphically()
        {
            // Arrange
            var activeUser = new ApplicationUser { IsActive = true };
            var inactiveUser = new ApplicationUser { IsActive = false };

            var accounts = new Account[]
            {
                new SavingsAccount 
                { 
                    IsActive = true, 
                    User = activeUser,
                    AccountNumber = "SAV123456789",
                    InterestRate = 0.05m,
                    InterestType = InterestType.Monthly,
                    UserId = "user1",
                    CurrencyId = 1,
                    CreatedDate = DateTime.UtcNow
                },
                new CheckingAccount 
                { 
                    IsActive = true, 
                    User = inactiveUser,
                    AccountNumber = "CHK123456789",
                    OverdraftLimit = 200m,
                    UserId = "user2", 
                    CurrencyId = 1,
                    CreatedDate = DateTime.UtcNow
                }
            };

            // Act & Assert - Polymorphic validation
            Assert.True(accounts[0].CanPerformTransactions());  // Active account + active user
            Assert.False(accounts[1].CanPerformTransactions()); // Active account + inactive user
        }

        [Fact]
        public void SavingsAccount_InterestCalculation_ShouldWork()
        {
            // Arrange
            var savingsAccount = new SavingsAccount 
            { 
                Balance = 1000m,
                InterestRate = 0.05m, // 5% annual
                InterestType = InterestType.Monthly,
                AccountNumber = "SAV123456789",
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var dailyInterest = savingsAccount.CalculateInterest(30); // 30 days

            // Assert
            Assert.True(dailyInterest > 0);
            
            // Apply interest
            var initialBalance = savingsAccount.Balance;
            savingsAccount.ApplyInterest(dailyInterest, DateTime.UtcNow);
            
            Assert.Equal(initialBalance + dailyInterest, savingsAccount.Balance);
            Assert.Single(savingsAccount.InterestLogs);
        }
    }
}