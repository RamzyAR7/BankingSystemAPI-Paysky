using System.Threading.Tasks;
using Moq;
using Xunit;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Application.Interfaces.Specification;
using System.Collections.Generic;
using System.Linq;
using BankingSystemAPI.UnitTests.TestInfrastructure;

namespace BankingSystemAPI.UnitTests.UnitTests.Application.Authorization;

public class TransactionAuthorizationServiceTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IScopeResolver> _scopeResolverMock = new();
    private readonly Mock<ILogger<TransactionAuthorizationService>> _loggerMock = new();
    private readonly TransactionAuthorizationService _service;

    public TransactionAuthorizationServiceTests()
    {
        _service = new TransactionAuthorizationService(
            _currentUserMock.Object,
            _uowMock.Object,
            _scopeResolverMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CanInitiateTransfer_Self_Success()
    {
        int sourceAccountId = 1, targetAccountId = 2;
        string userId = "user1";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1, balance: 100m);
        account.UserId = userId;
        account.Id = sourceAccountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanInitiateTransfer_Admin_Success()
    {
        int sourceAccountId = 3, targetAccountId = 4;
        string userId = "admin";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client1", 1, balance: 100m);
        account.User = new ApplicationUser { BankId = 1, Id = "client1" };
        account.UserId = "client1";
        account.Id = sourceAccountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client1")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 1 });
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanInitiateTransfer_NonOwner_Failure()
    {
        int sourceAccountId = 5, targetAccountId = 6;
        string userId = "user2";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount("otheruser", 1, balance: 100m);
        account.UserId = "otheruser";
        account.Id = sourceAccountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.CannotUseOthersAccounts, result.Errors);
    }

    [Fact]
    public async Task CanInitiateTransfer_NonExistentAccount_Failure()
    {
        int sourceAccountId = 7, targetAccountId = 8;
        string userId = "user3";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync((Account)null!);
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.False(result.IsSuccess);
        Assert.Contains($"Source account with ID '{sourceAccountId}' not found.", result.Errors[0]);
    }

    [Fact]
    public async Task CanInitiateTransfer_Admin_NonClientOwner_Failure()
    {
        int sourceAccountId = 9, targetAccountId = 10;
        string userId = "admin2";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client2", 1, balance: 100m);
        account.User = new ApplicationUser { BankId = 2, Id = "client2" };
        account.UserId = "client2";
        account.Id = sourceAccountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client2")).ReturnsAsync(new ApplicationRole { Name = "Admin" });
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 2 });
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.False(result.IsSuccess);
        Assert.Contains("Transfers can only be initiated from Client-owned accounts.", result.Errors);
    }

    [Fact]
    public async Task CanInitiateTransfer_Admin_DifferentBank_Failure()
    {
        int sourceAccountId = 11, targetAccountId = 12;
        string userId = "admin3";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client3", 1, balance: 100m);
        account.User = new ApplicationUser { BankId = 3, Id = "client3" };
        account.UserId = "client3";
        account.Id = sourceAccountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client3")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 99 });
        // Act
        var result = await _service.CanInitiateTransferAsync(sourceAccountId, targetAccountId);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.BankIsolationPolicy, result.Errors);
    }

    [Fact]
    public async Task FilterTransactions_Self_ReturnsOwnTransactions()
    {
        string userId = "user4";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var acc1 = TestEntityFactory.CreateCheckingAccount(userId, 1, balance: 100m);
        var acc2 = TestEntityFactory.CreateCheckingAccount("other", 1, balance: 50m);
        acc1.UserId = userId;
        acc2.UserId = "other";
        var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, AccountTransactions = new List<AccountTransaction> { new AccountTransaction { Account = acc1, AccountId = acc1.Id } } },
            new Transaction { Id = 2, AccountTransactions = new List<AccountTransaction> { new AccountTransaction { Account = acc2, AccountId = acc2.Id } } }
        }.AsQueryable();
        var filtered = await _service.FilterTransactionsAsync(transactions, 1, 10);
        Assert.True(filtered.IsSuccess);
    }
}
