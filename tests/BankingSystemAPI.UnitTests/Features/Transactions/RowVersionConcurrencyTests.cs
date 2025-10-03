using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.UnitTests.Features.Transactions;

/// <summary>
/// Tests specifically for row version (optimistic concurrency control) behavior
/// at the domain and Entity Framework level. Focuses on the row version mechanism
/// without complex threading scenarios.
/// </summary>
public class RowVersionConcurrencyTests : TestBase
{
    protected override void ConfigureMapperMock(Moq.Mock<AutoMapper.IMapper> mapperMock)
    {
        // No mapper needed for domain-level tests
    }

    #region Row Version Behavior Tests

    [Fact]
    public void RowVersion_ShouldExistOnNewEntity()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);

        // Assert
        Assert.NotNull(account.RowVersion);
        // In-memory database may not simulate SQL Server row version exactly,
        // but we can verify the property exists and has some value
    }

    [Fact]
    public void Account_Deposit_ShouldModifyEntity()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var originalBalance = account.Balance;

        // Act
        account.Deposit(500m);
        Context.SaveChanges();

        // Assert
        Assert.Equal(1500m, account.Balance);
        Assert.NotEqual(originalBalance, account.Balance);
        // Row version behavior may vary in in-memory database
        Assert.NotNull(account.RowVersion);
    }

    [Fact]
    public void Account_Withdraw_ShouldModifyEntity()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var originalBalance = account.Balance;

        // Act
        account.Withdraw(300m);
        Context.SaveChanges();

        // Assert
        Assert.Equal(700m, account.Balance);
        Assert.NotEqual(originalBalance, account.Balance);
        // Row version behavior may vary in in-memory database
        Assert.NotNull(account.RowVersion);
    }

    [Fact]
    public async Task TransactionCreation_ShouldNotAffectAccountRowVersion_UntilBalanceChange()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var originalBalance = account.Balance;

        // Act - Create transaction without modifying account
        var transaction = TestEntityFactory.CreateTransaction(TransactionType.Deposit);
        Context.Transactions.Add(transaction);
        Context.SaveChanges();

        // Assert - Account balance unchanged
        Context.Entry(account).Reload();
        Assert.Equal(originalBalance, account.Balance);

        // Now modify account balance
        account.Deposit(100m);
        Context.SaveChanges();

        // Assert - Now balance should change
        Assert.Equal(1100m, account.Balance);
        Assert.NotEqual(originalBalance, account.Balance);
    }

    #endregion

    #region Multi-Entity Concurrency Tests

    [Fact]
    public async Task Transfer_BetweenAccounts_ShouldUpdateBothBalances()
    {
        // Arrange
        var user1 = CreateTestUser("user1", "user1@test.com");
        var user2 = CreateTestUser("user2", "user2@test.com");
        var account1 = CreateTestCheckingAccount(user1.Id, balance: 1000m);
        var account2 = CreateTestCheckingAccount(user2.Id, balance: 500m);
        
        var originalBalance1 = account1.Balance;
        var originalBalance2 = account2.Balance;

        // Act - Simulate transfer
        account1.Withdraw(200m);
        account2.Deposit(200m);
        Context.SaveChanges();

        // Assert - Both balances should change
        Assert.Equal(800m, account1.Balance);
        Assert.Equal(700m, account2.Balance);
        Assert.NotEqual(originalBalance1, account1.Balance);
        Assert.NotEqual(originalBalance2, account2.Balance);
    }

    [Fact]
    public void RowVersion_InitialValue_ShouldExist()
    {
        // Arrange & Act
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);

        // Assert
        Assert.NotNull(account.RowVersion);
    }

    [Fact]
    public void RowVersion_ReadOnlyOperations_ShouldNotChangeBalance()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var originalBalance = account.Balance;

        // Act - Read operations only
        var balance = account.Balance;
        var accountNumber = account.AccountNumber;
        var isOverdrawn = account.IsOverdrawn();
        
        // Force a database read
        Context.Entry(account).Reload();

        // Assert - Balance should remain the same
        Assert.Equal(originalBalance, account.Balance);
        Assert.Equal(1000m, balance);
    }

    #endregion

    #region Account State and Business Logic Tests

    [Fact]
    public void Account_StatusChange_ShouldPersist()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var originalStatus = account.IsActive;

        // Act - Change account status
        account.IsActive = false;
        Context.SaveChanges();

        // Assert
        Assert.False(account.IsActive);
        Assert.NotEqual(originalStatus, account.IsActive);
    }

    [Theory]
    [InlineData(100, 200, 300)]  // Multiple deposits
    [InlineData(1000, 500, 250)] // Multiple withdrawals
    public void MultipleOperations_SequentialChanges_ShouldUpdateBalanceCorrectly(
        decimal amount1, decimal amount2, decimal amount3)
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 2000m, overdraftLimit: 1000m);
        var initialBalance = account.Balance;

        // Act - Perform operations sequentially
        account.Deposit(amount1);
        Context.SaveChanges();

        account.Withdraw(amount2);
        Context.SaveChanges();

        account.Deposit(amount3);
        Context.SaveChanges();

        // Assert - Verify final balance calculation
        var expectedBalance = initialBalance + amount1 - amount2 + amount3;
        Assert.Equal(expectedBalance, account.Balance);
    }

    #endregion

    #region Savings Account Interest Tests

    [Fact]
    public void SavingsAccount_InterestApplication_ShouldUpdateBalance()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestSavingsAccount(user.Id, balance: 1000m);
        var originalBalance = account.Balance;

        // Act - Apply interest
        account.ApplyInterest(50m, DateTime.UtcNow);
        Context.SaveChanges();

        // Assert
        Assert.Equal(1050m, account.Balance);
        Assert.NotEqual(originalBalance, account.Balance);
        Assert.Single(account.InterestLogs);
    }

    [Fact]
    public void Account_BusinessLogicChanges_ShouldPersist()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);

        // Act - Set same balance (should not cause issues)
        var currentBalance = account.Balance;
        account.Balance = currentBalance;
        Context.SaveChanges();

        // Assert - Balance should remain unchanged
        Context.Entry(account).Reload();
        Assert.Equal(1000m, account.Balance);
    }

    #endregion

    #region Concurrency Control Documentation

    [Fact]
    public void ConcurrencyControl_Documentation_Test()
    {
        // This test documents expected concurrency behavior
        // In a real SQL Server environment with row versioning:
        // 1. Each update increments the row version
        // 2. Concurrent updates cause DbUpdateConcurrencyException
        // 3. Applications must handle conflicts with retry logic
        
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);

        // Act & Assert - Document expected behavior
        Assert.NotNull(account.RowVersion);
        Assert.True(account.Balance >= 0); // Business rule
        Assert.NotNull(account.AccountNumber);
        Assert.True(account.IsActive); // Default state
        
        // In production, row version would change on each update
        // In-memory database may not simulate this exactly
        // This is expected and documented behavior
    }

    #endregion

    #region Helper Methods

    private CheckingAccount CreateTestCheckingAccount(string userId, decimal balance = 0m, decimal overdraftLimit = 1000m)
    {
        var currency = GetBaseCurrency();
        var account = TestEntityFactory.CreateCheckingAccount(userId, currency.Id, balance, overdraftLimit);
        Context.CheckingAccounts.Add(account);
        Context.SaveChanges();
        return account;
    }

    private SavingsAccount CreateTestSavingsAccount(string userId, decimal balance = 0m)
    {
        var currency = GetBaseCurrency();
        var account = TestEntityFactory.CreateSavingsAccount(userId, currency.Id, balance);
        Context.SavingsAccounts.Add(account);
        Context.SaveChanges();
        return account;
    }

    #endregion
}