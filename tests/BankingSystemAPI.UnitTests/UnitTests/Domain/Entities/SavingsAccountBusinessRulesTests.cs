#region Usings
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Xunit;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Domain.Entities;

/// <summary>
/// Tests for savings account business logic and interest calculations.
/// Focuses on complex business rules and domain behavior.
/// </summary>
public class SavingsAccountBusinessRulesTests
{
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    #region Interest Calculation Tests

    [Theory]
    [InlineData(1000, 0.05, 30, 4.11)]   // Monthly: 1000 * 0.05 / 365 * 30 = 4.11
    [InlineData(5000, 0.03, 90, 36.99)]  // Quarterly: 5000 * 0.03 / 365 * 90 = 36.99
    [InlineData(10000, 0.02, 365, 200)]  // Annual: 10000 * 0.02 / 365 * 365 = 200
    public void SavingsAccount_CalculateInterest_ShouldReturnCorrectAmount(
        decimal balance, decimal rate, int days, decimal expectedInterest)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: balance, interestRate: rate);

        // Act
        var interest = account.CalculateInterest(days);

        // Assert
        Assert.Equal(expectedInterest, Math.Round(interest, 2));
    }

    [Theory]
    [InlineData(InterestType.Monthly, 30)]
    [InlineData(InterestType.Quarterly, 90)]
    [InlineData(InterestType.Annually, 365)]
    public void SavingsAccount_ShouldApplyInterest_BasedOnInterestType(
        InterestType interestType, int daysSinceLastInterest)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, interestType: interestType);

        // Simulate time passed since last interest
        if (daysSinceLastInterest > 0)
        {
            var pastDate = DateTime.UtcNow.AddDays(-daysSinceLastInterest);
            account.ApplyInterest(1m, pastDate);
        }

        // Act
        var shouldApply = account.ShouldApplyInterest();

        // Assert - Should apply interest when enough time has passed
        Assert.True(shouldApply);
    }

    [Fact]
    public void SavingsAccount_ShouldApplyInterest_Every5Minutes_SpecialCase()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, interestType: InterestType.every5minutes);

        // Simulate 6 minutes passed
        var sixMinutesAgo = DateTime.UtcNow.AddMinutes(-6);
        account.ApplyInterest(1m, sixMinutesAgo);

        // Act
        var shouldApply = account.ShouldApplyInterest();

        // Assert - Should apply because more than 5 minutes have passed
        Assert.True(shouldApply);
    }

    [Fact]
    public void SavingsAccount_ApplyInterest_ShouldCreateInterestLog()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 1000m);
        var interestAmount = 25.50m;
        var timestamp = DateTime.UtcNow;

        // Act
        account.ApplyInterest(interestAmount, timestamp);

        // Assert
        Assert.Equal(1025.50m, account.Balance);
        Assert.Single(account.InterestLogs);

        var log = account.InterestLogs.First();
        Assert.Equal(interestAmount, log.Amount);
        Assert.Equal(timestamp.Date, log.Timestamp.Date);
        Assert.Equal(account.Id, log.SavingsAccountId);
        Assert.Equal(account.AccountNumber, log.SavingsAccountNumber);
    }

    [Fact]
    public void SavingsAccount_GetTotalInterestEarned_ShouldSumAllInterestLogs()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 1000m);

        // Act - Apply interest multiple times
        account.ApplyInterest(10m, DateTime.UtcNow.AddDays(-30));
        account.ApplyInterest(15m, DateTime.UtcNow.AddDays(-15));
        account.ApplyInterest(20m, DateTime.UtcNow);

        // Assert
        Assert.Equal(45m, account.GetTotalInterestEarned());
        Assert.Equal(1045m, account.Balance); // 1000 + 10 + 15 + 20
        Assert.Equal(3, account.InterestLogs.Count);
    }

    [Fact]
    public void SavingsAccount_GetLastInterestDate_ShouldReturnMostRecent()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1);
        var date1 = DateTime.UtcNow.AddDays(-30);
        var date2 = DateTime.UtcNow.AddDays(-15);
        var date3 = DateTime.UtcNow.AddDays(-5);

        // Act
        account.ApplyInterest(10m, date1);
        account.ApplyInterest(15m, date2);
        account.ApplyInterest(20m, date3);

        // Assert
        var lastDate = account.GetLastInterestDate();
        Assert.Equal(date3.Date, lastDate.Date);
    }

    [Fact]
    public void SavingsAccount_GetLastInterestDate_NoInterest_ShouldReturnCreatedDate()
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1);

        // Act
        var lastDate = account.GetLastInterestDate();

        // Assert
        Assert.Equal(account.CreatedDate.Date, lastDate.Date);
    }

    #endregion

    #region Interest Edge Cases

    [Theory]
    [InlineData(0)]      // Zero balance
    [InlineData(-5)]     // Negative days
    public void SavingsAccount_CalculateInterest_InvalidInputs_ShouldReturnZero(int days)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 0m);

        // Act
        var interest = account.CalculateInterest(days);

        // Assert
        Assert.Equal(0m, interest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void SavingsAccount_ApplyInterest_InvalidAmount_ShouldNotAffectAccount(decimal amount)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1, balance: 1000m);
        var originalBalance = account.Balance;
        var originalLogCount = account.InterestLogs.Count;

        // Act
        account.ApplyInterest(amount, DateTime.UtcNow);

        // Assert
        Assert.Equal(originalBalance, account.Balance);
        Assert.Equal(originalLogCount, account.InterestLogs.Count);
    }

    #endregion

    #region Interest Rate Scenarios

    [Theory]
    [InlineData(0.01, InterestType.Monthly)]    // Low rate, monthly
    [InlineData(0.10, InterestType.Quarterly)]  // High rate, quarterly  
    [InlineData(0.05, InterestType.Annually)]   // Medium rate, annually
    public void SavingsAccount_InterestCalculation_DifferentRatesAndTypes(
        decimal interestRate, InterestType interestType)
    {
        // Arrange
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1,
            balance: 10000m, interestRate: interestRate, interestType: interestType);

        // Act
        var monthlyInterest = account.CalculateInterest(30);
        var quarterlyInterest = account.CalculateInterest(90);
        var yearlyInterest = account.CalculateInterest(365);

        // Assert
        Assert.True(monthlyInterest >= 0);
        Assert.True(quarterlyInterest >= monthlyInterest);
        Assert.True(yearlyInterest >= quarterlyInterest);

        // Verify calculation: daily rate * days * balance
        var expectedMonthly = Math.Round(10000m * interestRate / 365m * 30m, 2);
        Assert.Equal(expectedMonthly, monthlyInterest);
    }

    #endregion

    #region Compound Interest Simulation

    [Fact]
    public void SavingsAccount_CompoundInterest_Simulation()
    {
        // Arrange - Simulate monthly compounding over a year
        var account = TestEntityFactory.CreateSavingsAccount("user1", 1,
            balance: 10000m, interestRate: 0.06m, interestType: InterestType.Monthly);

        var startingBalance = account.Balance;

        // Act - Apply monthly interest 12 times
        for (int month = 1; month <= 12; month++)
        {
            var monthlyInterest = account.CalculateInterest(30);
            account.ApplyInterest(monthlyInterest, DateTime.UtcNow.AddDays(month * 30));
        }

        // Assert
        var finalBalance = account.Balance;
        var totalInterest = account.GetTotalInterestEarned();

        Assert.True(finalBalance > startingBalance);
        Assert.Equal(12, account.InterestLogs.Count);

        // With monthly compounding, should earn more than simple annual interest
        var simpleInterest = startingBalance * 0.06m;
        Assert.True(totalInterest > simpleInterest);

        // Verify compound effect (each month's interest earns interest)
        Assert.True(finalBalance > startingBalance + simpleInterest);
    }

    #endregion
}
