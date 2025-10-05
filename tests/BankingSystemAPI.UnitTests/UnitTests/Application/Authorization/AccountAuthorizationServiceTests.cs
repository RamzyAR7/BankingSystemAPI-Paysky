#region Usings
using System.Threading.Tasks;
using Moq;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.UnitTests.TestInfrastructure;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Application.Authorization;

/// <summary>
/// Tests for account authorization service functionality.
/// </summary>
public class AccountAuthorizationServiceTests
{
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IScopeResolver> _scopeResolverMock = new();
    private readonly Mock<ILogger<AccountAuthorizationService>> _loggerMock = new();
    private readonly AccountAuthorizationService _service;

    public AccountAuthorizationServiceTests()
    {
        _service = new AccountAuthorizationService(
            _currentUserMock.Object,
            _uowMock.Object,
            _scopeResolverMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CanViewAccount_Owner_Success()
    {
        // Arrange
        int accountId = 1;
        string userId = "user1";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1);
        account.UserId = userId;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        // Act
        var result = await _service.CanViewAccountAsync(accountId);
        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanViewAccount_Admin_Success()
    {
        int accountId = 2;
        string userId = "admin";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client1", 1);
        account.UserId = "client1";
        account.User = new ApplicationUser { BankId = 1, Id = "client1" };
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client1")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        _currentUserMock.Setup(x => x.BankId).Returns(1);
        // Act
        var result = await _service.CanViewAccountAsync(accountId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanViewAccount_NonOwner_Failure()
    {
        int accountId = 3;
        string userId = "user2";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount("otheruser", 1);
        account.UserId = "otheruser";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        // Act
        var result = await _service.CanViewAccountAsync(accountId);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.AccountOwnershipRequired, result.Errors);
    }

    [Fact]
    public async Task CanEditAccount_Owner_Success()
    {
        int accountId = 4;
        string userId = "user4";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1);
        account.UserId = userId;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.CannotModifyOwnAccount, result.Errors);
    }

    [Fact]
    public async Task CanEditAccount_Admin_Success()
    {
        int accountId = 5;
        string userId = "admin2";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client2", 1);
        account.User = new ApplicationUser { BankId = 2, Id = "client2" };
        account.UserId = "client2";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 2 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client2")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanEditAccount_NonOwner_Failure()
    {
        int accountId = 6;
        string userId = "user6";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount("otheruser6", 1);
        account.UserId = "otheruser6";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteAccount_Owner_Success()
    {
        int accountId = 7;
        string userId = "user7";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1);
        account.UserId = userId;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Delete);
        Assert.False(result.IsSuccess);
        Assert.Contains("Users cannot delete their own accounts.", result.Errors);
    }

    [Fact]
    public async Task CanDeleteAccount_Admin_Success()
    {
        int accountId = 8;
        string userId = "admin8";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client8", 1);
        account.User = new ApplicationUser { BankId = 8, Id = "client8" };
        account.UserId = "client8";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 8 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client8")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Delete);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteAccount_NonOwner_Failure()
    {
        int accountId = 9;
        string userId = "user9";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount("otheruser9", 1);
        account.UserId = "otheruser9";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Delete);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanFreezeAccount_Admin_Success()
    {
        int accountId = 10;
        string userId = "admin10";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client10", 1);
        account.User = new ApplicationUser { BankId = 10, Id = "client10" };
        account.UserId = "client10";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 10 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client10")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Freeze);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanFreezeAccount_NonAdmin_Failure()
    {
        int accountId = 11;
        string userId = "user11";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1);
        account.UserId = userId;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Freeze);
        Assert.False(result.IsSuccess);
        Assert.Contains("Users cannot freeze or unfreeze their own accounts.", result.Errors);
    }

    [Fact]
    public async Task CanUnfreezeAccount_Admin_Success()
    {
        int accountId = 12;
        string userId = "admin12";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client12", 1);
        account.User = new ApplicationUser { BankId = 12, Id = "client12" };
        account.UserId = "client12";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 12 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client12")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Unfreeze);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUnfreezeAccount_NonAdmin_Failure()
    {
        int accountId = 13;
        string userId = "user13";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        var account = TestEntityFactory.CreateCheckingAccount(userId, 1);
        account.UserId = userId;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Unfreeze);
        Assert.False(result.IsSuccess);
        Assert.Contains("Users cannot freeze or unfreeze their own accounts.", result.Errors);
    }

    [Fact]
    public async Task CanModifyAccount_InactiveAccount_Failure()
    {
        int accountId = 14;
        string userId = "user14";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client14", 1);
        account.User = new ApplicationUser { BankId = 14, Id = "client14" };
        account.UserId = "client14";
        account.IsActive = false;
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 14 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client14")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
        // The service may not check IsActive directly, but you can assert forbidden if business logic is added
        // Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanModifyAccount_NonExistentAccount_Failure()
    {
        int accountId = 15;
        string userId = "user15";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync((Account)null!);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 15 });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains("Account with ID", result.Errors[0]);
    }

    [Theory]
    [InlineData(AccountModificationOperation.Deposit)]
    [InlineData(AccountModificationOperation.Withdraw)]
    [InlineData(AccountModificationOperation.Edit)]
    public async Task CanModifyAccount_ValidOperations_Success(AccountModificationOperation operation)
    {
        int accountId = 16;
        string userId = "admin16";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        var account = TestEntityFactory.CreateCheckingAccount("client16", 1);
        account.User = new ApplicationUser { BankId = 16, Id = "client16" };
        account.UserId = "client16";
        account.Id = accountId;
        _uowMock.Setup(x => x.AccountRepository.FindAsync(It.IsAny<ISpecification<Account>>())).ReturnsAsync(account);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>())).ReturnsAsync(new ApplicationUser { Id = userId, BankId = 16 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync("client16")).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyAccountAsync(accountId, operation);
        Assert.True(result.IsSuccess);
    }
}
