using AutoMapper;
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankingSystemAPI.UnitTests.Features.Transactions;

/// <summary>
/// Tests for transaction operations focusing on deposit functionality.
/// Simplified to avoid complex dependencies while maintaining test coverage.
/// </summary>
public class TransactionTests : TestBase
{
    private readonly DepositCommandHandler _depositHandler;

    public TransactionTests()
    {
        var depositLogger = new NullLogger<DepositCommandHandler>();
        _depositHandler = new DepositCommandHandler(UnitOfWork, Mapper, depositLogger);
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

    #region Deposit Tests

    [Theory]
    [InlineData(100)]
    [InlineData(0.01)]
    [InlineData(50000)]
    public async Task Deposit_ValidAmount_ShouldSucceed(decimal amount)
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 500m);
        var request = TestDtoBuilder.Deposit()
            .ToAccount(account.Id)
            .WithAmount(amount)
            .Build();

        // Act
        var result = await _depositHandler.Handle(new DepositCommand(request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Verify account balance updated
        Context.Entry(account).Reload();
        Assert.Equal(500m + amount, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Deposit_InvalidAmount_ShouldFail(decimal amount)
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id);
        var request = TestDtoBuilder.Deposit()
            .ToAccount(account.Id)
            .WithAmount(amount)
            .Build();

        // Act
        var result = await _depositHandler.Handle(new DepositCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Invalid amount"));
    }

    [Fact]
    public async Task Deposit_NonExistentAccount_ShouldFail()
    {
        // Arrange
        var request = TestDtoBuilder.Deposit()
            .ToAccount(999)
            .WithAmount(100m)
            .Build();

        // Act
        var result = await _depositHandler.Handle(new DepositCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task Deposit_InactiveAccount_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id);
        account.IsActive = false;
        Context.SaveChanges();

        var request = TestDtoBuilder.Deposit()
            .ToAccount(account.Id)
            .WithAmount(100m)
            .Build();

        // Act
        var result = await _depositHandler.Handle(new DepositCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("inactive"));
    }

    #endregion

    #region Helper Methods

    private CheckingAccount CreateTestCheckingAccount(string userId, decimal balance = 0m)
    {
        var currency = GetBaseCurrency();
        var account = TestEntityFactory.CreateCheckingAccount(userId, currency.Id, balance);
        Context.CheckingAccounts.Add(account);
        Context.SaveChanges();
        return account;
    }

    #endregion
}