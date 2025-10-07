#region Usings
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Xunit;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Domain.Entities;

/// <summary>
/// Tests for Account domain entity behavior and business rules.
/// Focuses on domain logic without external dependencies.
/// </summary>
public class AccountDomainTests
{
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    #region CheckingAccount Tests

    [Theory]
    [InlineData(1000, 500, 500)]      // Normal withdrawal
    [InlineData(1000, 1000, 0)]       // Withdraw all balance
    [InlineData(1000, 1500, -500)]    // Overdraft usage
    public void CheckingAccount_Withdraw_WithinOverdraftLimit_ShouldSucceed(
        decimal initialBalance, decimal withdrawAmount, decimal expectedBalance)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, initialBalance, overdraftLimit: 1000m);

        // Act
        account.Withdraw(withdrawAmount);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    [Fact]
    public void CheckingAccount_Withdraw_ExceedsOverdraftLimit_ShouldThrow()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 1000m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(2000m));
        Assert.Contains("Insufficient funds", exception.Message);
        Assert.Contains("Maximum withdrawal", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CheckingAccount_Withdraw_InvalidAmount_ShouldThrow(decimal amount)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 1000m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(amount));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-500, true)]
    [InlineData(100, false)]
    public void CheckingAccount_IsOverdrawn_ShouldReturnCorrectStatus(decimal balance, bool expectedOverdrawn)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: balance);

        // Act & Assert
        Assert.Equal(expectedOverdrawn, account.IsOverdrawn());
    }

    [Theory]
    [InlineData(-500, 500)]
    [InlineData(0, 0)]
    [InlineData(100, 0)]
    public void CheckingAccount_GetOverdraftUsed_ShouldReturnCorrectAmount(decimal balance, decimal expectedUsed)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: balance);

        // Act & Assert
        Assert.Equal(expectedUsed, account.GetOverdraftUsed());
    }

    [Fact]
    public void CheckingAccount_OverdraftMethods_ShouldCalculateCorrectly()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user123", 1, balance: 100m, overdraftLimit: 200m);

        // Act & Assert - Test different overdraft scenarios

        // Scenario 1: Positive balance
        Assert.Equal(100m, account.GetAvailableBalance()); // Actual balance
        Assert.Equal(300m, account.GetMaxWithdrawalAmount()); // 100 + 200 overdraft
        Assert.True(account.CanWithdraw(250m)); // Within limit
        Assert.False(account.CanWithdraw(350m)); // Exceeds limit
        Assert.False(account.IsOverdrawn());
        Assert.Equal(0m, account.GetOverdraftUsed());
        Assert.Equal(200m, account.GetAvailableOverdraftCredit());

        // Scenario 2: After withdrawal into overdraft
        account.Withdraw(150m); // 100 - 150 = -50 (overdrawn)

        Assert.Equal(-50m, account.GetAvailableBalance()); // Actual balance (negative)
        Assert.Equal(150m, account.GetMaxWithdrawalAmount()); // 200 - 50 used = 150 remaining credit
        Assert.True(account.CanWithdraw(100m)); // Within remaining credit
        Assert.False(account.CanWithdraw(200m)); // Exceeds remaining credit
        Assert.True(account.IsOverdrawn());
        Assert.Equal(50m, account.GetOverdraftUsed());
        Assert.Equal(150m, account.GetAvailableOverdraftCredit());

        // Scenario 3: At overdraft limit
        account.Withdraw(150m); // -50 - 150 = -200 (at limit)

        Assert.Equal(-200m, account.GetAvailableBalance()); // At overdraft limit
        Assert.Equal(0m, account.GetMaxWithdrawalAmount()); // No more credit available
        Assert.False(account.CanWithdraw(0.01m)); // Cannot withdraw even small amount
        Assert.True(account.IsOverdrawn());
        Assert.Equal(200m, account.GetOverdraftUsed()); // Full overdraft used
        Assert.Equal(0m, account.GetAvailableOverdraftCredit()); // No credit left
    }

    [Fact]
    public void CheckingAccount_BalanceVsOverdraftCredit_ConceptualSeparation()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user123", 1, balance: 500m, overdraftLimit: 300m);

        // Act & Assert - Demonstrate conceptual separation

        // Customer's actual money vs bank's credit facility
        Assert.Equal(500m, account.GetAvailableBalance()); // Customer's money 
        Assert.Equal(300m, account.OverdraftLimit); // Bank's credit facility 
        Assert.Equal(800m, account.GetMaxWithdrawalAmount()); // Total available for withdrawal 

        // After using overdraft - customer owes the bank
        account.Withdraw(600m); // Uses 100 of overdraft

        Assert.Equal(-100m, account.GetAvailableBalance()); // Customer owes bank 100 
        Assert.Equal(100, account.GetOverdraftUsed()); // Amount borrowed from bank 
        Assert.Equal(200, account.GetAvailableOverdraftCredit()); // Bank can still lend 200 more 
        Assert.Equal(200, account.GetMaxWithdrawalAmount()); // Can still withdraw 200 more 

        // Account status provides clear information
        var status = account.GetAccountStatus();
        Assert.Contains("Overdrawn", status);
        Assert.Contains("100", status); // Shows used overdraft amount (without negative sign)
        Assert.Contains("300", status); // Shows total overdraft limit
    }

    [Theory]
    [InlineData(1000, 500, 1500)] // Positive balance: balance + overdraft
    [InlineData(0, 300, 300)]     // Zero balance: full overdraft available
    [InlineData(-100, 500, 400)]  // Overdrawn: remaining credit only
    [InlineData(-500, 500, 0)]    // At limit: no credit left
    public void CheckingAccount_MaxWithdrawalCalculation_ShouldBeCorrect(
        decimal balance, decimal overdraftLimit, decimal expectedMaxWithdrawal)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user123", 1, balance: balance, overdraftLimit: overdraftLimit);

        // Act
        var maxWithdrawal = account.GetMaxWithdrawalAmount();

        // Assert
        Assert.Equal(expectedMaxWithdrawal, maxWithdrawal);
    }

    #endregion

    #region SavingsAccount Tests

    [Fact]
    public void SavingsAccount_Withdraw_SufficientBalance_ShouldSucceed()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 1000m);

        // Act
        account.Withdraw(300m);

        // Assert
        Assert.Equal(700m, account.Balance);
    }

    [Fact]
    public void SavingsAccount_Withdraw_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 500m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(600m));
        Assert.Contains("Insufficient funds", exception.Message);
    }

    [Theory]
    [InlineData(1000, 0.05, 30, InterestType.Monthly)]
    [InlineData(5000, 0.03, 90, InterestType.Quarterly)]
    [InlineData(10000, 0.02, 365, InterestType.Annually)]
    public void SavingsAccount_CalculateInterest_ShouldReturnCorrectAmount(
        decimal balance, decimal rate, int days, InterestType interestType)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: balance, interestRate: rate, interestType: interestType);

        // Act
        var interest = account.CalculateInterest(days);

        // Assert
        Assert.True(interest > 0);
        Assert.Equal(Math.Round(balance * rate / 365m * days, 2), interest);
    }

    [Theory]
    [InlineData(0, 0.05, 30)]
    [InlineData(1000, 0.05, 0)]
    [InlineData(1000, 0.05, -10)]
    public void SavingsAccount_CalculateInterest_InvalidInputs_ShouldReturnZero(
        decimal balance, decimal rate, int days)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: balance, interestRate: rate);

        // Act
        var interest = account.CalculateInterest(days);

        // Assert
        Assert.Equal(0m, interest);
    }

    [Fact]
    public void SavingsAccount_ApplyInterest_ShouldUpdateBalanceAndLog()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 1000m);
        var interestAmount = 50m;
        var calculationDate = DateTime.UtcNow;

        // Act
        account.ApplyInterest(interestAmount, calculationDate);

        // Assert
        Assert.Equal(1050m, account.Balance);
        Assert.Single(account.InterestLogs);
        Assert.Equal(interestAmount, account.InterestLogs.First().Amount);
        Assert.Equal(calculationDate.Date, account.InterestLogs.First().Timestamp.Date);
    }

    [Theory]
    [InlineData(InterestType.Monthly, 30, true)]
    [InlineData(InterestType.Monthly, 25, false)]
    [InlineData(InterestType.Quarterly, 90, true)]
    [InlineData(InterestType.Quarterly, 89, false)]
    [InlineData(InterestType.Annually, 365, true)]
    public void SavingsAccount_ShouldApplyInterest_ShouldReturnCorrectResult(
        InterestType interestType, int daysSinceLastInterest, bool expectedResult)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, interestType: interestType);

        // Simulate time passed since last interest
        if (daysSinceLastInterest > 0)
        {
            var lastInterestDate = DateTime.UtcNow.AddDays(-daysSinceLastInterest);
            account.ApplyInterest(10m, lastInterestDate);
        }

        // Act
        var result = account.ShouldApplyInterest();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void SavingsAccount_ShouldApplyInterest_Every5Minutes_WithTimePassed()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, interestType: InterestType.every5minutes);

        // Simulate 6 minutes ago
        var sixMinutesAgo = DateTime.UtcNow.AddMinutes(-6);
        account.ApplyInterest(1m, sixMinutesAgo);

        // Act
        var result = account.ShouldApplyInterest();

        // Assert - Should return true because more than 5 minutes have passed
        Assert.True(result);
    }

    [Fact]
    public void SavingsAccount_ShouldApplyInterest_Every5Minutes_NoTimePassed()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, interestType: InterestType.every5minutes);

        // No previous interest applied - account just created

        // Act
        var result = account.ShouldApplyInterest();

        // Assert - Should return false because no time has passed since creation
        Assert.False(result);
    }

    #endregion

    #region Common Account Tests

    [Theory]
    [InlineData(100)]
    [InlineData(0.01)]
    [InlineData(1000000)]
    public void Account_Deposit_ValidAmount_ShouldIncreaseBalance(decimal amount)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);
        var initialBalance = account.Balance;

        // Act
        account.Deposit(amount);

        // Assert
        Assert.Equal(initialBalance + amount, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Account_Deposit_InvalidAmount_ShouldThrow(decimal amount)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Deposit(amount));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void Account_CanPerformTransactions_ActiveAccountAndUser_ShouldReturnTrue()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1);
        account.IsActive = true;
        account.User = new ApplicationUser { IsActive = true };

        // Act & Assert
        Assert.True(account.CanPerformTransactions());
    }

    [Theory]
    [InlineData(false, true)]  // Inactive account, active user
    [InlineData(true, false)]  // Active account, inactive user
    [InlineData(false, false)] // Both inactive
    public void Account_CanPerformTransactions_InactiveStates_ShouldReturnFalse(
        bool accountActive, bool userActive)
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1);
        account.IsActive = accountActive;
        account.User = new ApplicationUser { IsActive = userActive };

        // Act & Assert
        Assert.False(account.CanPerformTransactions());
    }

    [Fact]
    public void Deposit_ValidAmount_UpdatesBalance()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);
        var amountToDeposit = 250m;
        var expectedBalance = 750m;

        // Act
        account.Deposit(amountToDeposit);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    [Fact]
    public void Deposit_NegativeAmount_Throws()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);
        var negativeAmount = -100m;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Deposit(negativeAmount));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void Withdraw_ValidAmount_UpdatesBalance()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);
        var amountToWithdraw = 200m;
        var expectedBalance = 300m;

        // Act
        account.Withdraw(amountToWithdraw);

        // Assert
        Assert.Equal(expectedBalance, account.Balance);
    }

    #endregion

    #region Additional Tests

    [Fact]
    public void Deposit_ZeroAmount_Throws()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Deposit(0));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void Withdraw_ZeroAmount_Throws()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(0));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void Withdraw_NegativeAmount_Throws()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(-100));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void Deposit_AccountWithOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 300m);

        // Act
        account.Deposit(100m); // Deposit should succeed even with overdraft

        // Assert
        Assert.Equal(600m, account.Balance);
    }

    [Fact]
    public void Withdraw_AccountWithOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 300m);

        // Act
        account.Withdraw(700m); // Withdraw 500 + 200 (within overdraft)

        // Assert
        Assert.Equal(-200m, account.Balance); // Balance should be -200 (200 overdraft used)
    }

    [Fact]
    public void Deposit_AccountWithMaxOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 1000m);

        // Act
        account.Deposit(500m); // Deposit should succeed, increasing balance to 1000

        // Assert
        Assert.Equal(1000m, account.Balance);
    }

    [Fact]
    public void Withdraw_AccountWithMaxOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 1000m);

        // Act
        account.Withdraw(1200m); // Withdraw 500 + 700 (within max overdraft)

        // Assert
        Assert.Equal(-700m, account.Balance); // Balance should be -700 (700 overdraft used)
    }

    [Fact]
    public void Deposit_AccountWithMinOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 100m);

        // Act
        account.Deposit(100m); // Deposit should succeed, increasing balance to 600

        // Assert
        Assert.Equal(600m, account.Balance);
    }

    [Fact]
    public void Withdraw_AccountWithMinOverdraft_Succeeds()
    {
        // Arrange
        var account = TestEntityFactory.CreateCheckingAccount("user1", 1, balance: 500m, overdraftLimit: 100m);

        // Act
        account.Withdraw(550m); // Withdraw 500 + 50 (within min overdraft)

        // Assert
        Assert.Equal(-50m, account.Balance); // Balance should be -50 (50 overdraft used)
    }

    #endregion
}
