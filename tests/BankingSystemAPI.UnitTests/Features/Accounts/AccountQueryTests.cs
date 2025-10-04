using AutoMapper;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId;
using BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Moq;

namespace BankingSystemAPI.UnitTests.Features.Accounts;

/// <summary>
/// Tests for Account query and management operations.
/// Focuses on polymorphism and account lifecycle management.
/// </summary>
public class AccountQueryTests : TestBase
{
    private readonly GetAccountByIdQueryHandler _getByIdHandler;
    private readonly GetAccountByAccountNumberQueryHandler _getByNumberHandler;
    private readonly GetAccountsByUserIdQueryHandler _getByUserHandler;
    private readonly DeleteAccountCommandHandler _deleteHandler;

    public AccountQueryTests()
    {
        var mockAccountAuth = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IAccountAuthorizationService>();
        mockAccountAuth.Setup(x => x.CanViewAccountAsync(It.IsAny<int>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Success());
        mockAccountAuth.Setup(x => x.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<BankingSystemAPI.Domain.Constant.AccountModificationOperation>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Success());
        mockAccountAuth.Setup(x => x.FilterAccountsQueryAsync(It.IsAny<IQueryable<BankingSystemAPI.Domain.Entities.Account>>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result<IQueryable<BankingSystemAPI.Domain.Entities.Account>>.Success(It.IsAny<IQueryable<BankingSystemAPI.Domain.Entities.Account>>()));

        var mockUserAuth = new Mock<BankingSystemAPI.Application.Interfaces.Authorization.IUserAuthorizationService>();
        mockUserAuth.Setup(x => x.CanViewUserAsync(It.IsAny<string>()))
            .ReturnsAsync(BankingSystemAPI.Domain.Common.Result.Success());

        _getByIdHandler = new GetAccountByIdQueryHandler(UnitOfWork, Mapper, mockAccountAuth.Object);
        _getByNumberHandler = new GetAccountByAccountNumberQueryHandler(UnitOfWork, Mapper, mockAccountAuth.Object);
        _getByUserHandler = new GetAccountsByUserIdQueryHandler(UnitOfWork, Mapper, mockAccountAuth.Object, mockUserAuth.Object);
        _deleteHandler = new DeleteAccountCommandHandler(UnitOfWork, mockAccountAuth.Object);
    }

    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns((Account a) => new AccountDto 
            { 
                Id = a.Id, 
                AccountNumber = a.AccountNumber, 
                Balance = a.Balance, 
                UserId = a.UserId, 
                CurrencyCode = a.Currency?.Code ?? "USD",
                Type = a.GetType().Name
            });

        mapperMock.Setup(m => m.Map<IEnumerable<AccountDto>>(It.IsAny<IEnumerable<Account>>()))
            .Returns((IEnumerable<Account> accounts) => accounts.Select(a => new AccountDto 
            { 
                Id = a.Id, 
                AccountNumber = a.AccountNumber, 
                Balance = a.Balance, 
                UserId = a.UserId, 
                CurrencyCode = a.Currency?.Code ?? "USD",
                Type = a.GetType().Name
            }));
    }

    #region Query Tests

    [Fact]
    public async Task GetAccountById_ValidId_ShouldReturnAccount()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id);

        // Act
        var result = await _getByIdHandler.Handle(new GetAccountByIdQuery(account.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(account.AccountNumber, result.Value.AccountNumber);
        Assert.Equal("CheckingAccount", result.Value.Type);
    }

    [Fact]
    public async Task GetAccountById_InvalidId_ShouldFail()
    {
        // Act
        var result = await _getByIdHandler.Handle(new GetAccountByIdQuery(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAccountByNumber_ValidNumber_ShouldReturnAccount()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id);

        // Act
        var result = await _getByNumberHandler.Handle(
            new GetAccountByAccountNumberQuery(account.AccountNumber), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(account.Id, result.Value.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NONEXISTENT")]
    public async Task GetAccountByNumber_InvalidNumber_ShouldFail(string accountNumber)
    {
        // Act
        var result = await _getByNumberHandler.Handle(
            new GetAccountByAccountNumberQuery(accountNumber), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAccountsByUserId_ValidUser_ShouldReturnUserAccounts()
    {
        // Arrange
        var user = CreateTestUser();
        var checking = CreateTestCheckingAccount(user.Id);
        var savings = CreateTestSavingsAccount(user.Id);

        // Act
        var result = await _getByUserHandler.Handle(new GetAccountsByUserIdQuery(user.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
        Assert.Contains(result.Value, a => a.Type == "CheckingAccount");
        Assert.Contains(result.Value, a => a.Type == "SavingsAccount");
    }

    [Fact]
    public async Task GetAccountsByUserId_NonExistentUser_ShouldReturnEmpty()
    {
        // Act
        var result = await _getByUserHandler.Handle(
            new GetAccountsByUserIdQuery("nonexistent-user"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAccount_ZeroBalance_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 0m);

        // Act
        var result = await _deleteHandler.Handle(new DeleteAccountCommand(account.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAccount_WithBalance_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser();
        var account = CreateTestCheckingAccount(user.Id, balance: 100m);

        // Act
        var result = await _deleteHandler.Handle(new DeleteAccountCommand(account.Id), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("positive balance"));
    }

    [Fact]
    public async Task DeleteAccount_NonExistent_ShouldFail()
    {
        // Act
        var result = await _deleteHandler.Handle(new DeleteAccountCommand(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
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