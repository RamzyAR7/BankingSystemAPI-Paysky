using BankingSystemAPI.Application.Authorization;
using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    /// <summary>
    /// Tests to verify that users cannot modify themselves (edit/delete) but can change their own password.
    /// This is a critical security requirement to prevent privilege escalation.
    /// </summary>
    public class SelfModificationTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<ILogger<UserAuthorizationService>> _mockLogger;
        private readonly UserAuthorizationService _authorizationService;

        public SelfModificationTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockLogger = new Mock<ILogger<UserAuthorizationService>>();

            _authorizationService = new UserAuthorizationService(
                _mockCurrentUserService.Object,
                _mockUnitOfWork.Object,
                _mockScopeResolver.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CanModifyUserAsync_SelfEdit_ShouldReturnForbidden()
        {
            // Arrange
            var userId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Same as in the JWT token
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser
            {
                Id = userId,
                UserName = "alexjones",
                Email = "alex.jones@example.com",
                RoleId = "admin-role-id"
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);

            // Act
            var result = await _authorizationService.CanModifyUserAsync(userId, UserModificationOperation.Edit);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Users cannot edit their own profile details", result.Errors.First());
        }

        [Fact]
        public async Task CanModifyUserAsync_SelfDelete_ShouldReturnForbidden()
        {
            // Arrange
            var userId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90";
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser
            {
                Id = userId,
                UserName = "alexjones",
                Email = "alex.jones@example.com",
                RoleId = "admin-role-id"
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);

            // Act
            var result = await _authorizationService.CanModifyUserAsync(userId, UserModificationOperation.Delete);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Users cannot delete themselves.", result.Errors);
        }

        [Fact]
        public async Task CanModifyUserAsync_SelfPasswordChange_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90";
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser
            {
                Id = userId,
                UserName = "alexjones",
                Email = "alex.jones@example.com",
                RoleId = "admin-role-id"
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);

            // Act
            var result = await _authorizationService.CanModifyUserAsync(userId, UserModificationOperation.ChangePassword);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanModifyUserAsync_DifferentUser_ShouldReturnSuccess()
        {
            // Arrange
            var currentUserId = "current-user-id";
            var targetUserId = "different-user-id";
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(currentUserId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockCurrentUser = new ApplicationUser
            {
                Id = currentUserId,
                UserName = "currentuser",
                Email = "current@example.com",
                RoleId = "admin-role-id"
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockCurrentUser);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);

            // Act
            var result = await _authorizationService.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);

            // Assert
            Assert.True(result.IsSuccess);
        }
    }
}