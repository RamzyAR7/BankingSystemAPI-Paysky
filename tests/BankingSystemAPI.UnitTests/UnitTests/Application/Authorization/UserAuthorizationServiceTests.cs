#region Usings
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
#endregion
namespace BankingSystemAPI.UnitTests.UnitTests.Application.Authorization;

public class UserAuthorizationServiceTests
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
    private readonly Mock<ILogger<UserAuthorizationService>> _loggerMock = new();
    private readonly UserAuthorizationService _service;

    public UserAuthorizationServiceTests()
    {
        _service = new UserAuthorizationService(
            _currentUserMock.Object,
            _uowMock.Object,
            _scopeResolverMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CanViewUser_Self_Success()
    {
        string userId = "user1";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        // Act
        var result = await _service.CanViewUserAsync(userId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanViewUser_Admin_Success()
    {
        string userId = "admin";
        string targetUserId = "client1";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, BankId = 1 });
        _currentUserMock.Setup(x => x.BankId).Returns(1);
        // Act
        var result = await _service.CanViewUserAsync(targetUserId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanViewUser_OtherUser_Failure()
    {
        string userId = "user2";
        string targetUserId = "otheruser";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        // Act
        var result = await _service.CanViewUserAsync(targetUserId);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked, result.Errors);
    }

    [Fact]
    public async Task CanEditUser_Self_Failure()
    {
        string userId = "user3";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyUserAsync(userId, UserModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.CannotModifySelf, result.Errors);
    }

    [Fact]
    public async Task CanEditUser_Admin_Success()
    {
        string userId = "admin2";
        string targetUserId = "client2";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        // Ensure acting user then target user are returned in sequence
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, BankId = 2 })
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, BankId = 2 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanEditUser_OtherUser_Failure()
    {
        string userId = "user4";
        string targetUserId = "otheruser4";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked, result.Errors);
    }

    [Fact]
    public async Task CanDeleteUser_Admin_Success()
    {
        string userId = "admin3";
        string targetUserId = "client3";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, BankId = 3 })
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, BankId = 3 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Delete);
        Assert.True(result.IsSuccess, string.Join(" | ", result.Errors));
    }

    [Fact]
    public async Task CanDeleteUser_NonAdmin_Failure()
    {
        string userId = "user5";
        string targetUserId = "otheruser5";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Delete);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked, result.Errors);
    }

    [Fact]
    public async Task CanChangePassword_Self_Success()
    {
        string userId = "user6";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyUserAsync(userId, UserModificationOperation.ChangePassword);
        Assert.True(result.IsSuccess, string.Join(" | ", result.Errors));
    }

    [Fact]
    public async Task CanChangePassword_Admin_Success()
    {
        string userId = "admin4";
        string targetUserId = "client4";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, BankId = 4 })
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, BankId = 4 });
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.ChangePassword);
        Assert.True(result.IsSuccess, string.Join(" | ", result.Errors));
    }

    [Fact]
    public async Task CanChangePassword_OtherUser_Failure()
    {
        string userId = "user7";
        string targetUserId = "otheruser7";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);
        _uowMock.Setup(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = userId });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.ChangePassword);
        Assert.False(result.IsSuccess);
        Assert.Contains(AuthorizationConstants.ErrorMessages.ClientsModifyOthersBlocked, result.Errors);
    }

    [Fact]
    public async Task CanModifyUser_InactiveUser_Failure()
    {
        string userId = "admin5";
        string targetUserId = "inactiveuser";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, BankId = 5 })
            .ReturnsAsync((ApplicationUser)null!);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains($"Target user with ID '{targetUserId}' not found.", result.Errors[0]);
    }

    [Fact]
    public async Task CanModifyUser_NonExistentUser_Failure()
    {
        string userId = "admin6";
        string targetUserId = "nonexistentuser";
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _scopeResolverMock.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);
        _uowMock.SetupSequence(x => x.UserRepository.FindAsync(It.IsAny<ISpecification<ApplicationUser>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, BankId = 6 })
            .ReturnsAsync((ApplicationUser)null!);
        _uowMock.Setup(x => x.RoleRepository.GetRoleByUserIdAsync(targetUserId)).ReturnsAsync(new ApplicationRole { Name = "Client" });
        // Act
        var result = await _service.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);
        Assert.False(result.IsSuccess);
        Assert.Contains($"Target user with ID '{targetUserId}' not found.", result.Errors[0]);
    }
}

