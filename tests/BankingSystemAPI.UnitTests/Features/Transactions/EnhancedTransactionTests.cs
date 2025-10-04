using AutoMapper;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankingSystemAPI.UnitTests.Features.Transactions;

/// <summary>
/// Enhanced transaction tests focusing on business logic, validation,
/// and sequential operation scenarios that test transaction integrity.
/// </summary>
public class EnhancedTransactionTests : TestBase
{
    private readonly DepositCommandHandler _depositHandler;
    private readonly WithdrawCommandHandler _withdrawHandler;

    public EnhancedTransactionTests()
    {
        var depositLogger = new NullLogger<DepositCommandHandler>();
        var mockAccountAuth = new Mock<IAccountAuthorizationService>();
        mockAccountAuth.Setup(x => x.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>()))
            .ReturnsAsync(Result.Success());
        
        _depositHandler = new DepositCommandHandler(UnitOfWork, Mapper, depositLogger, mockAccountAuth.Object);
        _withdrawHandler = new WithdrawCommandHandler(UnitOfWork, Mapper, mockAccountAuth.Object);
    }

    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        mapperMock.Setup(m => m.Map<TransactionResDto>(It.IsAny<Transaction>()))
            .Returns((Transaction t) => new TransactionResDto 
            { 
                TransactionType = t.TransactionType.ToString(),
                Timestamp = t.Timestamp,
                Amount = t.AccountTransactions?.FirstOrDefault()?.Amount ?? 0m
            });
    }

    #region Sequential Transaction Tests

    [Fact]
    public async Task SequentialDeposits_ShouldUpdateBalanceCorrectly()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        
        // Act - Perform multiple deposits in sequence
        var deposit1 = await PerformDeposit(account.Id, 100m);
        var deposit2 = await PerformDeposit(account.Id, 200m);
        var deposit3 = await PerformDeposit(account.Id, 50m);

        // Assert
        Assert.True(deposit1.IsSuccess);
        Assert.True(deposit2.IsSuccess);
        Assert.True(deposit3.IsSuccess);
        
        Context.Entry(account).Reload();
        Assert.Equal(1350m, account.Balance); // 1000 + 100 + 200 + 50
    }

    [Fact]
    public async Task SequentialWithdrawals_WithinOverdraftLimit_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m, overdraftLimit: 300m);
        
        // Verify initial state
        Assert.Equal(500m, account.GetAvailableBalance()); // Actual balance
        Assert.Equal(800m, account.GetMaxWithdrawalAmount()); // 500 + 300 overdraft
        
        // First withdrawal: 300 from 500 = 200 remaining
        var withdraw1 = await PerformWithdraw(account.Id, 300m);
        Assert.True(withdraw1.IsSuccess, $"First withdrawal should succeed. Error: {string.Join(", ", withdraw1.Errors)}");
        Context.Entry(account).Reload();
        Assert.Equal(200m, account.Balance);
        Assert.Equal(500m, account.GetMaxWithdrawalAmount()); // 200 + 300 overdraft
        
        // Second withdrawal: 400 from 200 balance (uses 200 overdraft)
        // Max allowed: 200 (balance) + 300 (overdraft) = 500, so 400 should work
        var withdraw2 = await PerformWithdraw(account.Id, 400m);
        Assert.True(withdraw2.IsSuccess, $"Second withdrawal should succeed. Error: {string.Join(", ", withdraw2.Errors)}");
        
        Context.Entry(account).Reload();
        Assert.Equal(-200m, account.Balance); // 200 - 400 = -200 (using overdraft)
        Assert.True(account.IsOverdrawn());
        Assert.Equal(200m, account.GetOverdraftUsed()); // Used 200 of 300 overdraft
        Assert.Equal(100m, account.GetAvailableOverdraftCredit()); // 300 - 200 = 100 remaining
    }

    [Fact]
    public async Task CheckingAccount_ExceedOverdraftLimit_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 100m, overdraftLimit: 200m);
        
        // Act - Try to withdraw more than balance + overdraft
        // Max withdrawal: 100 (balance) + 200 (overdraft) = 300
        var result = await PerformWithdraw(account.Id, 350m); // Exceeds limit

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Insufficient funds"));
        Assert.Contains(result.Errors, e => e.Contains("300")); // Should mention max withdrawal amount
        
        // Balance should remain unchanged
        Context.Entry(account).Reload();
        Assert.Equal(100m, account.Balance);
        Assert.False(account.IsOverdrawn());
    }

    [Theory]
    [InlineData(1000, 100, 50, 200, 1250)]  // 1000 + 100 - 50 + 200 = 1250
    [InlineData(500, 0, 300, 100, 300)]     // 500 + 0 - 300 + 100 = 300  
    [InlineData(0, 1000, 200, 0, 800)]      // 0 + 1000 - 200 + 0 = 800
    public async Task MixedTransactions_ShouldMaintainCorrectBalance(
        decimal initialBalance, decimal deposit1, decimal withdraw1, decimal deposit2, decimal expectedFinal)
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: initialBalance, overdraftLimit: 1000m);

        // Act
        if (deposit1 > 0)
        {
            var d1 = await PerformDeposit(account.Id, deposit1);
            Assert.True(d1.IsSuccess, $"Deposit 1 failed: {string.Join(", ", d1.Errors)}");
        }
        
        if (withdraw1 > 0)
        {
            var w1 = await PerformWithdraw(account.Id, withdraw1);
            Assert.True(w1.IsSuccess, $"Withdraw 1 failed: {string.Join(", ", w1.Errors)}");
        }
        
        if (deposit2 > 0)
        {
            var d2 = await PerformDeposit(account.Id, deposit2);
            Assert.True(d2.IsSuccess, $"Deposit 2 failed: {string.Join(", ", d2.Errors)}");
        }

        // Assert
        Context.Entry(account).Reload();
        Assert.Equal(expectedFinal, account.Balance);
    }

    #endregion

    #region Account Type Specific Tests

    [Fact]
    public async Task SavingsAccount_WithdrawAllFunds_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestSavingsAccount(user.Id, balance: 1000m);
        
        // Act - Withdraw all funds
        var result = await PerformWithdraw(account.Id, 1000m);

        // Assert
        Assert.True(result.IsSuccess);
        
        Context.Entry(account).Reload();
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public async Task SavingsAccount_CannotOverdraw_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestSavingsAccount(user.Id, balance: 500m);
        
        // Act - Try to withdraw more than balance
        var result = await PerformWithdraw(account.Id, 600m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Insufficient funds"));
        
        Context.Entry(account).Reload();
        Assert.Equal(500m, account.Balance);
    }

    [Fact]
    public async Task CheckingAccount_OverdraftFeatures_ShouldWorkCorrectly()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 200m, overdraftLimit: 300m);

        // Act & Assert - Test overdraft functionality step by step
        
        // Verify initial state
        Assert.Equal(200m, account.GetAvailableBalance()); // Actual balance
        Assert.Equal(500m, account.GetMaxWithdrawalAmount()); // Balance + overdraft
        Assert.False(account.IsOverdrawn());
        
        // Normal withdrawal: 150 from 200 = 50 remaining
        var withdraw1 = await PerformWithdraw(account.Id, 150m);
        Assert.True(withdraw1.IsSuccess, $"First withdrawal failed: {string.Join(", ", withdraw1.Errors)}");
        Context.Entry(account).Reload();
        Assert.Equal(50m, account.Balance);
        Assert.Equal(50m, account.GetAvailableBalance()); // Still actual balance
        Assert.Equal(350m, account.GetMaxWithdrawalAmount()); // 50 + 300 overdraft
        Assert.False(account.IsOverdrawn());

        // Overdraft withdrawal: 200 from 50 balance using overdraft
        // Max withdrawal: 50 (balance) + 300 (overdraft) = 350, so 200 should work
        var withdraw2 = await PerformWithdraw(account.Id, 200m);
        Assert.True(withdraw2.IsSuccess, $"Second withdrawal failed: {string.Join(", ", withdraw2.Errors)}");
        Context.Entry(account).Reload();
        Assert.Equal(-150m, account.Balance); // 50 - 200 = -150 (overdrawn)
        Assert.Equal(-150m, account.GetAvailableBalance()); // Actual balance (negative)
        Assert.True(account.IsOverdrawn());
        Assert.Equal(150m, account.GetOverdraftUsed());
        Assert.Equal(150m, account.GetAvailableOverdraftCredit()); // 300 - 150 used

        // Try to withdraw more than remaining overdraft credit (should fail)
        var withdraw3 = await PerformWithdraw(account.Id, 200m); // Only 150 credit left
        Assert.False(withdraw3.IsSuccess, "Third withdrawal should fail - exceeds overdraft");

        // Deposit to recover: -150 + 300 = 150
        var deposit = await PerformDeposit(account.Id, 300m);
        Assert.True(deposit.IsSuccess, $"Deposit failed: {string.Join(", ", deposit.Errors)}");
        Context.Entry(account).Reload();
        Assert.Equal(150m, account.Balance); // -150 + 300 = 150
        Assert.Equal(150m, account.GetAvailableBalance()); // Back to positive
        Assert.False(account.IsOverdrawn());
        Assert.Equal(0m, account.GetOverdraftUsed());
        Assert.Equal(300m, account.GetAvailableOverdraftCredit()); // Full overdraft available again
    }

    #endregion

    #region Business Rule Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public async Task InvalidAmounts_ShouldFailValidation(decimal amount)
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);

        // Act & Assert - Both deposit and withdraw should fail
        var depositResult = await PerformDeposit(account.Id, amount);
        var withdrawResult = await PerformWithdraw(account.Id, amount);

        Assert.False(depositResult.IsSuccess);
        Assert.False(withdrawResult.IsSuccess);
        
        // Balance should remain unchanged
        Context.Entry(account).Reload();
        Assert.Equal(1000m, account.Balance);
    }

    [Fact]
    public async Task InactiveAccount_ShouldRejectTransactions()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        account.IsActive = false;
        Context.SaveChanges();

        // Act
        var depositResult = await PerformDeposit(account.Id, 100m);
        var withdrawResult = await PerformWithdraw(account.Id, 50m);

        // Assert
        Assert.False(depositResult.IsSuccess);
        Assert.False(withdrawResult.IsSuccess);
        
        Assert.Contains(depositResult.Errors, e => e.Contains("inactive"));
        Assert.Contains(withdrawResult.Errors, e => e.Contains("inactive"));
        
        Context.Entry(account).Reload();
        Assert.Equal(1000m, account.Balance); // Unchanged
    }

    [Fact]
    public async Task InactiveUser_ShouldRejectTransactions()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        
        user.IsActive = false;
        Context.SaveChanges();

        // Act
        var depositResult = await PerformDeposit(account.Id, 100m);
        var withdrawResult = await PerformWithdraw(account.Id, 50m);

        // Assert
        Assert.False(depositResult.IsSuccess);
        Assert.False(withdrawResult.IsSuccess);
        
        Context.Entry(account).Reload();
        Assert.Equal(1000m, account.Balance); // Unchanged
    }

    [Fact]
    public async Task NonExistentAccount_ShouldFailGracefully()
    {
        // Act
        var depositResult = await PerformDeposit(999, 100m);
        var withdrawResult = await PerformWithdraw(999, 50m);

        // Assert
        Assert.False(depositResult.IsSuccess);
        Assert.False(withdrawResult.IsSuccess);
        
        Assert.Contains(depositResult.Errors, e => e.Contains("not found"));
        Assert.Contains(withdrawResult.Errors, e => e.Contains("not found"));
    }

    #endregion

    #region Decimal Precision Tests

    [Fact]
    public async Task PrecisionHandling_SmallAmounts_ShouldRoundCorrectly()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 0m);

        // Act - Multiple small precision transactions
        await PerformDeposit(account.Id, 0.01m);
        await PerformDeposit(account.Id, 0.02m);
        await PerformDeposit(account.Id, 0.03m);
        await PerformWithdraw(account.Id, 0.04m);

        // Assert
        Context.Entry(account).Reload();
        Assert.Equal(0.02m, account.Balance); // 0.01 + 0.02 + 0.03 - 0.04 = 0.02
    }

    [Fact]
    public async Task PrecisionHandling_LargeAmounts_ShouldMaintainAccuracy()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 0m);

        // Act - Large amounts with decimal places
        await PerformDeposit(account.Id, 1234567.89m);
        await PerformWithdraw(account.Id, 234567.12m);

        // Assert
        Context.Entry(account).Reload();
        Assert.Equal(1000000.77m, account.Balance);
    }

    #endregion

    #region Transaction History and Audit Tests

    [Fact]
    public async Task MultipleTransactions_ShouldCreateCorrectHistory()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 1000m);
        var startTime = DateTime.UtcNow;

        // Act - Perform multiple transactions
        var deposit = await PerformDeposit(account.Id, 200m);
        var withdraw = await PerformWithdraw(account.Id, 150m);
        var deposit2 = await PerformDeposit(account.Id, 75m);

        // Assert - Verify transactions were created
        Assert.True(deposit.IsSuccess);
        Assert.True(withdraw.IsSuccess);
        Assert.True(deposit2.IsSuccess);

        // Verify transaction timestamps
        Assert.True(deposit.Value.Timestamp >= startTime);
        Assert.True(withdraw.Value.Timestamp >= deposit.Value.Timestamp);
        Assert.True(deposit2.Value.Timestamp >= withdraw.Value.Timestamp);

        // Verify transaction types
        Assert.Equal("Deposit", deposit.Value.TransactionType);
        Assert.Equal("Withdraw", withdraw.Value.TransactionType);
        Assert.Equal("Deposit", deposit2.Value.TransactionType);

        // Verify amounts
        Assert.Equal(200m, deposit.Value.Amount);
        Assert.Equal(150m, withdraw.Value.Amount);
        Assert.Equal(75m, deposit2.Value.Amount);

        // Final balance check
        Context.Entry(account).Reload();
        Assert.Equal(1125m, account.Balance); // 1000 + 200 - 150 + 75
    }

    #endregion

    #region Helper Methods

    private async Task<Result<TransactionResDto>> PerformDeposit(int accountId, decimal amount)
    {
        var request = TestDtoBuilder.Deposit()
            .ToAccount(accountId)
            .WithAmount(amount)
            .Build();

        return await _depositHandler.Handle(new DepositCommand(request), CancellationToken.None);
    }

    private async Task<Result<TransactionResDto>> PerformWithdraw(int accountId, decimal amount)
    {
        var request = TestDtoBuilder.Withdraw()
            .FromAccount(accountId)
            .WithAmount(amount)
            .Build();

        return await _withdrawHandler.Handle(new WithdrawCommand(request), CancellationToken.None);
    }

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