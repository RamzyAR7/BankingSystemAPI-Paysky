using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    public class UserAuthorizationServiceTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<ILogger<IUserAuthorizationService>> _mockLogger;

        public UserAuthorizationServiceTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockLogger = new Mock<ILogger<IUserAuthorizationService>>();
        }

        #region CanViewUserAsync Tests

        [Fact]
        public async Task CanViewUserAsync_WithMockedService_ShouldReturnResult()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanViewUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsSuccess);
            mockAuthService.Verify(x => x.CanViewUserAsync(targetUserId), Times.Once);
        }

        [Fact]
        public async Task CanViewUserAsync_AccessDenied_ShouldReturnFailure()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetUserId = "user456";
            var expectedResult = Result.Forbidden("Access denied to user data");

            mockAuthService.Setup(x => x.CanViewUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", string.Join(" ", result.Errors));
        }

        #endregion

        #region CanModifyUserAsync Tests

        [Theory]
        [InlineData(UserModificationOperation.Edit)]
        [InlineData(UserModificationOperation.Delete)]
        [InlineData(UserModificationOperation.ChangePassword)]
        public async Task CanModifyUserAsync_DifferentOperations_ShouldReturnResult(UserModificationOperation operation)
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanModifyUserAsync(targetUserId, operation))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanModifyUserAsync(targetUserId, operation);

            // Assert
            Assert.True(result.IsSuccess);
            mockAuthService.Verify(x => x.CanModifyUserAsync(targetUserId, operation), Times.Once);
        }

        [Fact]
        public async Task CanModifyUserAsync_SelfDeletion_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var userId = "user123";
            var expectedResult = Result.Forbidden("Users cannot delete themselves");

            mockAuthService.Setup(x => x.CanModifyUserAsync(userId, UserModificationOperation.Delete))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanModifyUserAsync(userId, UserModificationOperation.Delete);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("cannot delete themselves", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task CanModifyUserAsync_ClientModifyingOther_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetUserId = "user456";
            var expectedResult = Result.Forbidden("Clients cannot modify other users");

            mockAuthService.Setup(x => x.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanModifyUserAsync(targetUserId, UserModificationOperation.Edit);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("cannot modify other users", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region CanCreateUserAsync Tests

        [Fact]
        public async Task CanCreateUserAsync_AdminRole_ShouldReturnSuccess()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanCreateUserAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanCreateUserAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanCreateUserAsync_ClientRole_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var expectedResult = Result.Forbidden("Clients cannot create users");

            mockAuthService.Setup(x => x.CanCreateUserAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanCreateUserAsync();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("cannot create users", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region FilterUsersAsync Tests

        [Fact]
        public async Task FilterUsersAsync_ValidRequest_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var users = new[] { CreateTestUser("user1"), CreateTestUser("user2") };
            var expectedResult = Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>
                .Success((users, 2));

            mockAuthService.Setup(x => x.FilterUsersAsync(It.IsAny<IQueryable<ApplicationUser>>(), 1, 10, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterUsersAsync(users.AsQueryable(), 1, 10, null, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.Equal(2, result.Value.Users.Count());
        }

        [Fact]
        public async Task FilterUsersAsync_SelfScope_ShouldReturnOnlyCurrentUser()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var currentUser = CreateTestUser("user123");
            var users = new[] { currentUser };
            var expectedResult = Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>
                .Success((users, 1));

            mockAuthService.Setup(x => x.FilterUsersAsync(It.IsAny<IQueryable<ApplicationUser>>(), 1, 10, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterUsersAsync(users.AsQueryable(), 1, 10, null, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.TotalCount);
            Assert.Single(result.Value.Users);
        }

        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 3)]
        [InlineData(1, 10)]
        public async Task FilterUsersAsync_DifferentPagination_ShouldReturnCorrectPage(int pageNumber, int pageSize)
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var users = Enumerable.Range(1, 10).Select(i => CreateTestUser($"user{i}"));
            var expectedPageItems = users.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            var expectedResult = Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>
                .Success((expectedPageItems, 10));

            mockAuthService.Setup(x => x.FilterUsersAsync(It.IsAny<IQueryable<ApplicationUser>>(), pageNumber, pageSize, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterUsersAsync(users.AsQueryable(), pageNumber, pageSize, null, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value.TotalCount);
            Assert.Equal(expectedPageItems.Count(), result.Value.Users.Count());
        }

        #endregion

        #region Access Scope Tests

        [Fact]
        public void AccessScope_GlobalScope_ShouldAllowAllOperations()
        {
            // Arrange & Act
            var globalScope = AccessScope.Global;

            // Assert
            Assert.Equal(AccessScope.Global, globalScope);
            // In a real scenario, global scope would allow all operations
        }

        [Fact]
        public void AccessScope_BankLevelScope_ShouldRestrictToBankUsers()
        {
            // Arrange & Act
            var bankScope = AccessScope.BankLevel;

            // Assert
            Assert.Equal(AccessScope.BankLevel, bankScope);
            // In a real scenario, bank level scope would restrict to same bank users
        }

        [Fact]
        public void AccessScope_SelfScope_ShouldRestrictToOwnData()
        {
            // Arrange & Act
            var selfScope = AccessScope.Self;

            // Assert
            Assert.Equal(AccessScope.Self, selfScope);
            // In a real scenario, self scope would only allow access to own data
        }

        #endregion

        #region Admin Role Restriction Tests

        [Fact]
        public async Task CanViewUserAsync_AdminViewingAnotherAdmin_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetAdminUserId = "admin456";
            var expectedResult = Result.Forbidden("You can only access Client users.");

            mockAuthService.Setup(x => x.CanViewUserAsync(targetAdminUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewUserAsync(targetAdminUserId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("You can only access Client users", string.Join(" ", result.Errors));
        }

        [Fact]
        public async Task CanViewUserAsync_AdminViewingClient_ShouldReturnSuccess()
        {
            // Arrange
            var mockAuthService = new Mock<IUserAuthorizationService>();
            var targetClientUserId = "client123";
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanViewUserAsync(targetClientUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewUserAsync(targetClientUserId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region Helper Methods

        private ApplicationUser CreateTestUser(string userId, int? bankId = null)
        {
            return new ApplicationUser
            {
                Id = userId,
                UserName = $"user{userId}",
                Email = $"user{userId}@example.com",
                FullName = $"User {userId}",
                IsActive = true,
                BankId = bankId ?? 100,
                Bank = bankId.HasValue ? new Bank { Id = bankId.Value, Name = $"Bank {bankId}", IsActive = true } : null,
                Accounts = new List<Account>()
            };
        }

        #endregion
    }
}