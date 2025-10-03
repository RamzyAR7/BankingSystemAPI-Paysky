using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;

namespace BankingSystemAPI.UnitTests.Domain;

/// <summary>
/// Tests for transaction business rules and domain logic.
/// Focuses on complex transaction scenarios and edge cases.
/// </summary>
public class TransactionBusinessTests
{
    #region Balance Calculation Tests

    [Theory]
    [InlineData(1000, 500, 1500)]      // Normal deposit
    [InlineData(0, 100, 100)]          // Deposit to zero balance
    [InlineData(1000, 0.01, 1000.01)]  // Minimal deposit
    public void Account_Deposit_ShouldUpdateBalanceCorrectly(
        decimal initialBalance, decimal depositAmount, decimal expectedBalance)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: initialBalance);

        // Act
        account.Deposit(depositAmount);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    [Theory]
    [InlineData(1000, 500, 500)]       // Normal withdrawal
    [InlineData(1000, 1000, 0)]        // Withdraw all
    [InlineData(500, 1000, -500)]      // Overdraft (checking account)
    public void CheckingAccount_Withdraw_ShouldUpdateBalanceCorrectly(
        decimal initialBalance, decimal withdrawAmount, decimal expectedBalance)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, 
            balance: initialBalance, overdraftLimit: 1000m);

        // Act
        account.Withdraw(withdrawAmount);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    #endregion

    #region Overdraft Tests

    [Theory]
    [InlineData(500, 1000, 1000, true)]   // Within overdraft limit
    [InlineData(500, 1600, 1000, false)]  // Exceeds overdraft limit
    [InlineData(0, 500, 1000, true)]      // Use overdraft from zero
    [InlineData(-500, 600, 1000, false)]  // Already overdrawn, exceed limit
    public void CheckingAccount_Withdraw_OverdraftValidation(
        decimal initialBalance, decimal withdrawAmount, decimal overdraftLimit, bool shouldSucceed)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, 
            balance: initialBalance, overdraftLimit: overdraftLimit);

        // Act & Assert
        if (shouldSucceed)
        {
            account.Withdraw(withdrawAmount);
            Assert.Equal(initialBalance - withdrawAmount, account.Balance);
        }
        else
        {
            var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(withdrawAmount));
            Assert.Contains("Insufficient funds", exception.Message);
        }
    }

    [Theory]
    [InlineData(100, false, 0)]      // Positive balance
    [InlineData(0, false, 0)]        // Zero balance
    [InlineData(-100, true, 100)]    // Negative balance
    [InlineData(-500, true, 500)]    // Deep overdraft
    public void CheckingAccount_OverdraftStatus_ShouldBeCorrect(
        decimal balance, bool expectedOverdrawn, decimal expectedUsed)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: balance);

        // Act & Assert
        Assert.Equal(expectedOverdrawn, account.IsOverdrawn());
        Assert.Equal(expectedUsed, account.GetOverdraftUsed());
    }

    #endregion

    #region Account State Validation

    [Theory]
    [InlineData(true, true, true)]    // Both active
    [InlineData(false, true, false)]  // Inactive account
    [InlineData(true, false, false)]  // Inactive user
    [InlineData(false, false, false)] // Both inactive
    public void Account_CanPerformTransactions_ShouldValidateActiveState(
        bool accountActive, bool userActive, bool expectedCanTransact)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1);
        account.IsActive = accountActive;
        account.User = new ApplicationUser 
        { 
            IsActive = userActive,
            Id = "user1"
        };

        // Act & Assert
        Assert.Equal(expectedCanTransact, account.CanPerformTransactions());
    }

    #endregion

    #region Transaction Type Validation

    [Fact] 
    public void SavingsAccount_Withdraw_ExceedsBalance_ShouldThrow()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 100m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(150m));
        Assert.Contains("Insufficient funds", exception.Message);
    }

    [Fact]
    public void SavingsAccount_NoOverdraft_UnlikeChecking()
    {
        // Arrange
        var savingsAccount = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 100m);
        var checkingAccount = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 100m, overdraftLimit: 500m);

        // Act & Assert - Savings should not allow overdraft
        Assert.Throws<InvalidOperationException>(() => savingsAccount.Withdraw(150m));
        
        // Checking should allow overdraft
        checkingAccount.Withdraw(150m);
        Assert.Equal(-50m, checkingAccount.Balance);
    }

    #endregion

    #region Currency and Precision Tests

    [Theory]
    [InlineData(1000.123, 1000.12)]   // Round down
    [InlineData(1000.126, 1000.13)]   // Round up
    [InlineData(1000.125, 1000.12)]   // Banker's rounding
    public void Account_BalanceRounding_ShouldRoundToTwoDecimals(decimal inputAmount, decimal expectedBalance)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 0m);

        // Act
        account.Deposit(inputAmount);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    [Fact]
    public void Account_MultipleTransactions_ShouldMaintainPrecision()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 1000m);

        // Act - Multiple small transactions
        account.Deposit(0.01m);
        account.Deposit(0.01m);
        account.Deposit(0.01m);
        account.Withdraw(0.01m);

        // Assert
        Assert.Equal(1000.02m, account.Balance);
    }

    #endregion

    #region Account Type Polymorphism

    [Fact]
    public void Account_AccountType_ShouldReturnCorrectType()
    {
        // Arrange
        var checkingAccount = TestEntityFactory.CreateCheckingAccount("user1", 1);
        var savingsAccount = TestEntityFactory.CreateSavingsAccount("user1", 1);

        // Act & Assert
        Assert.Equal(AccountType.Checking, checkingAccount.AccountType);
        Assert.Equal(AccountType.Savings, savingsAccount.AccountType);
    }

    [Fact]
    public void Account_PolymorphicBehavior_ShouldWorkCorrectly()
    {
        // Arrange
        Account[] accounts = [
            TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 100m, overdraftLimit: 500m),
            TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 100m)
        ];

        // Act & Assert - Polymorphic deposit should work for both
        foreach (var account in accounts)
        {
            account.Deposit(50m);
            Assert.Equal(150m, account.Balance);
        }

        // Withdraw behavior should differ
        var checking = (CheckingAccount)accounts[0];
        var savings = (SavingsAccount)accounts[1];

        // Checking allows overdraft
        checking.Withdraw(200m); // Should succeed (balance becomes -50)
        Assert.Equal(-50m, checking.Balance);

        // Savings does not allow overdraft
        Assert.Throws<InvalidOperationException>(() => savings.Withdraw(200m));
        Assert.Equal(150m, savings.Balance); // Unchanged
    }

    #endregion
}