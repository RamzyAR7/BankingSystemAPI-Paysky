using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Xunit;

namespace BankingSystemAPI.UnitTests.Domain
{
    /// <summary>
    /// Tests to verify Liskov Substitution Principle compliance for Account hierarchy
    /// </summary>
    public class AccountLspTests
    {
        [Theory]
        [InlineData(typeof(SavingsAccount))]
        [InlineData(typeof(CheckingAccount))]
        public void Account_Polymorphic_WithdrawBehavior_ShouldWork(Type accountType)
        {
            // Arrange
            Account account = accountType.Name switch
            {
                nameof(SavingsAccount) => new SavingsAccount 
                { 
                    Id = 1, 
                    Balance = 1000m, 
                    InterestRate = 0.05m,
                    InterestType = InterestType.Monthly,
                    AccountNumber = "SAV123456789",
                    UserId = "user1",
                    CurrencyId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                nameof(CheckingAccount) => new CheckingAccount 
                { 
                    Id = 2, 
                    Balance = 1000m, 
                    OverdraftLimit = 500m,
                    AccountNumber = "CHK123456789",
                    UserId = "user2",
                    CurrencyId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                _ => throw new ArgumentException("Invalid account type")
            };

            // Act & Assert - Polymorphic behavior should work
            var availableBalance = account.GetAvailableBalance();
            
            // Both should allow withdrawal within available balance
            Assert.True(availableBalance > 0);
            
            // Should be able to withdraw without knowing specific type
            account.Withdraw(100m);
            
            // Balance should be updated correctly
            Assert.Equal(900m, account.Balance);
        }

        [Fact]
        public void CheckingAccount_ShouldAllowOverdraft_WhileSavingsAccount_ShouldNot()
        {
            // Arrange
            var savings = new SavingsAccount 
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
            
            var checking = new CheckingAccount 
            { 
                Balance = 100m, 
                OverdraftLimit = 200m,
                AccountNumber = "CHK123456789",
                UserId = "user2",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert
            // Savings should not allow overdraft
            Assert.Equal(100m, savings.GetAvailableBalance());
            Assert.Throws<InvalidOperationException>(() => savings.Withdraw(150m));

            // Checking should allow overdraft
            Assert.Equal(300m, checking.GetAvailableBalance());
            checking.Withdraw(150m); // Should succeed
            Assert.Equal(-50m, checking.Balance);
        }

        [Fact]
        public void Accounts_ShouldMaintain_ConsistentExceptionBehavior()
        {
            // Arrange
            var savings = new SavingsAccount 
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
            
            var checking = new CheckingAccount 
            { 
                Balance = 100m, 
                OverdraftLimit = 50m, 
                AccountNumber = "CHK123456789",
                UserId = "user2",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert - Both should throw same exception type for invalid amounts
            Assert.Throws<InvalidOperationException>(() => savings.Withdraw(-10m));
            Assert.Throws<InvalidOperationException>(() => checking.Withdraw(-10m));
            
            Assert.Throws<InvalidOperationException>(() => savings.Withdraw(0m));
            Assert.Throws<InvalidOperationException>(() => checking.Withdraw(0m));
        }

        [Fact]
        public void Account_Deposit_ShouldWork_Polymorphically()
        {
            // Arrange
            Account[] accounts = new Account[]
            {
                new SavingsAccount 
                { 
                    Balance = 500m, 
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
                    Balance = 500m, 
                    OverdraftLimit = 200m,
                    AccountNumber = "CHK123456789",
                    UserId = "user2",
                    CurrencyId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            // Act & Assert - Polymorphic deposit should work for both
            foreach (var account in accounts)
            {
                var initialBalance = account.Balance;
                account.Deposit(100m);
                Assert.Equal(initialBalance + 100m, account.Balance);
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

            // Act & Assert
            Assert.True(accounts[0].CanPerformTransactions()); // Active account with active user
            Assert.False(accounts[1].CanPerformTransactions()); // Active account with inactive user
        }

        [Fact]
        public void CheckingAccount_OverdraftMethods_ShouldWork()
        {
            // Arrange
            var checkingAccount = new CheckingAccount
            {
                Balance = -50m, // Overdrawn
                OverdraftLimit = 200m,
                AccountNumber = "CHK123456789",
                UserId = "user1",
                CurrencyId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act & Assert
            Assert.True(checkingAccount.IsOverdrawn());
            Assert.Equal(50m, checkingAccount.GetOverdraftUsed());
            Assert.Equal(150m, checkingAccount.GetRemainingOverdraft());
            Assert.Equal(150m, checkingAccount.GetAvailableBalance());
        }
    }
}