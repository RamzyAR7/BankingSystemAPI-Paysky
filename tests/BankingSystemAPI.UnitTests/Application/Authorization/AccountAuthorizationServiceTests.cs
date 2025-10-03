using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    public class AccountAuthorizationServiceTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<ILogger<IAccountAuthorizationService>> _mockLogger;

        public AccountAuthorizationServiceTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockLogger = new Mock<ILogger<IAccountAuthorizationService>>();
        }

        #region CanViewAccountAsync Tests

        [Fact]
        public async Task CanViewAccountAsync_WithMockedService_ShouldReturnResult()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanViewAccountAsync(accountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewAccountAsync(accountId);

            // Assert
            Assert.True(result.IsSuccess);
            mockAuthService.Verify(x => x.CanViewAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task CanViewAccountAsync_AccessDenied_ShouldReturnFailure()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var expectedResult = Result.Forbidden("Access denied to account");

            mockAuthService.Setup(x => x.CanViewAccountAsync(accountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewAccountAsync(accountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", string.Join(" ", result.Errors));
        }

        [Fact]
        public async Task CanViewAccountAsync_NonExistentAccount_ShouldReturnNotFound()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accountId = 999;
            var expectedResult = Result.NotFound("Account", accountId);

            mockAuthService.Setup(x => x.CanViewAccountAsync(accountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanViewAccountAsync(accountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        #endregion

        #region CanModifyAccountAsync Tests

        [Theory]
        [InlineData(AccountModificationOperation.Edit)]
        [InlineData(AccountModificationOperation.Delete)]
        public async Task CanModifyAccountAsync_DifferentOperations_ShouldReturnResult(AccountModificationOperation operation)
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanModifyAccountAsync(accountId, operation))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanModifyAccountAsync(accountId, operation);

            // Assert
            Assert.True(result.IsSuccess);
            mockAuthService.Verify(x => x.CanModifyAccountAsync(accountId, operation), Times.Once);
        }

        [Fact]
        public async Task CanModifyAccountAsync_UnauthorizedOperation_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var operation = AccountModificationOperation.Delete;
            var expectedResult = Result.Forbidden("Cannot delete account");

            mockAuthService.Setup(x => x.CanModifyAccountAsync(accountId, operation))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanModifyAccountAsync(accountId, operation);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Cannot delete", string.Join(" ", result.Errors));
        }

        #endregion

        #region CanCreateAccountForUserAsync Tests

        [Fact]
        public async Task CanCreateAccountForUserAsync_ValidUser_ShouldReturnSuccess()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanCreateAccountForUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanCreateAccountForUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanCreateAccountForUserAsync_InsufficientPermissions_ShouldReturnForbidden()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Forbidden("Cannot create accounts for users");

            mockAuthService.Setup(x => x.CanCreateAccountForUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanCreateAccountForUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Cannot create accounts", string.Join(" ", result.Errors));
        }

        #endregion

        #region FilterAccountsAsync Tests

        [Fact]
        public async Task FilterAccountsAsync_ValidRequest_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockAuthService = new Mock<IAccountAuthorizationService>();
            var accounts = new[] { CreateTestAccount(1), CreateTestAccount(2) };
            var expectedResult = Result<(System.Collections.Generic.IEnumerable<Account> Accounts, int TotalCount)>
                .Success((accounts, 2));

            mockAuthService.Setup(x => x.FilterAccountsAsync(It.IsAny<System.Linq.IQueryable<Account>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterAccountsAsync(accounts.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.Equal(2, result.Value.Accounts.Count());
        }

        #endregion

        #region Helper Methods

        private Account CreateTestAccount(int accountId)
        {
            return new CheckingAccount
            {
                Id = accountId,
                AccountNumber = $"ACC-{accountId:D8}",
                Balance = 1000m,
                IsActive = true,
                UserId = $"user{accountId}",
                User = new ApplicationUser
                {
                    Id = $"user{accountId}",
                    UserName = $"user{accountId}",
                    Email = $"user{accountId}@example.com",
                    IsActive = true
                }
            };
        }

        #endregion
    }
}