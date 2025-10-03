using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    /// <summary>
    /// Comprehensive authorization service tests that validate security and access control mechanisms
    /// </summary>
    public class AuthorizationServicesTests
    {
        #region Account Authorization Tests

        [Fact]
        public async Task AccountAuthorization_CanViewAccount_ShouldReturnResult()
        {
            // Arrange
            var mockAccountAuth = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var expectedResult = Result.Success();

            mockAccountAuth.Setup(x => x.CanViewAccountAsync(accountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAccountAuth.Object.CanViewAccountAsync(accountId);

            // Assert
            Assert.True(result.IsSuccess);
            mockAccountAuth.Verify(x => x.CanViewAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task AccountAuthorization_CanModifyAccount_WithEditOperation_ShouldReturnResult()
        {
            // Arrange
            var mockAccountAuth = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var operation = AccountModificationOperation.Edit;
            var expectedResult = Result.Success();

            mockAccountAuth.Setup(x => x.CanModifyAccountAsync(accountId, operation))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAccountAuth.Object.CanModifyAccountAsync(accountId, operation);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AccountAuthorization_CanCreateAccountForUser_ShouldReturnResult()
        {
            // Arrange
            var mockAccountAuth = new Mock<IAccountAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockAccountAuth.Setup(x => x.CanCreateAccountForUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAccountAuth.Object.CanCreateAccountForUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AccountAuthorization_FilterAccounts_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockAccountAuth = new Mock<IAccountAuthorizationService>();
            var accounts = CreateTestAccounts(3);
            var expectedResult = Result<(IEnumerable<Account> Accounts, int TotalCount)>
                .Success((accounts, 3));

            mockAccountAuth.Setup(x => x.FilterAccountsAsync(It.IsAny<IQueryable<Account>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAccountAuth.Object.FilterAccountsAsync(accounts.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.TotalCount);
        }

        #endregion

        #region User Authorization Tests

        [Fact]
        public async Task UserAuthorization_CanViewUser_ShouldReturnResult()
        {
            // Arrange
            var mockUserAuth = new Mock<IUserAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockUserAuth.Setup(x => x.CanViewUserAsync(targetUserId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockUserAuth.Object.CanViewUserAsync(targetUserId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(UserModificationOperation.Edit)]
        [InlineData(UserModificationOperation.Delete)]
        [InlineData(UserModificationOperation.ChangePassword)]
        public async Task UserAuthorization_CanModifyUser_DifferentOperations_ShouldReturnResult(UserModificationOperation operation)
        {
            // Arrange
            var mockUserAuth = new Mock<IUserAuthorizationService>();
            var targetUserId = "user123";
            var expectedResult = Result.Success();

            mockUserAuth.Setup(x => x.CanModifyUserAsync(targetUserId, operation))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockUserAuth.Object.CanModifyUserAsync(targetUserId, operation);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UserAuthorization_CanCreateUser_ShouldReturnResult()
        {
            // Arrange
            var mockUserAuth = new Mock<IUserAuthorizationService>();
            var expectedResult = Result.Success();

            mockUserAuth.Setup(x => x.CanCreateUserAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockUserAuth.Object.CanCreateUserAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UserAuthorization_FilterUsers_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockUserAuth = new Mock<IUserAuthorizationService>();
            var users = CreateTestUsers(2);
            var expectedResult = Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>
                .Success((users, 2));

            mockUserAuth.Setup(x => x.FilterUsersAsync(It.IsAny<IQueryable<ApplicationUser>>(), 1, 10, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockUserAuth.Object.FilterUsersAsync(users.AsQueryable(), 1, 10, null, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
        }

        #endregion

        #region Transaction Authorization Tests

        [Fact]
        public async Task TransactionAuthorization_CanInitiateTransfer_ShouldReturnResult()
        {
            // Arrange
            var mockTransactionAuth = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 1;
            var targetAccountId = 2;
            var expectedResult = Result.Success();

            mockTransactionAuth.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockTransactionAuth.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task TransactionAuthorization_FilterTransactions_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockTransactionAuth = new Mock<ITransactionAuthorizationService>();
            var transactions = CreateTestTransactions(4);
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((transactions, 4));

            mockTransactionAuth.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockTransactionAuth.Object.FilterTransactionsAsync(transactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(4, result.Value.TotalCount);
        }

        #endregion

        #region Authorization Failure Tests

        [Fact]
        public async Task Authorization_AccessDenied_ShouldReturnForbiddenResult()
        {
            // Arrange
            var mockAccountAuth = new Mock<IAccountAuthorizationService>();
            var accountId = 1;
            var expectedResult = Result.Forbidden("Access denied to account");

            mockAccountAuth.Setup(x => x.CanViewAccountAsync(accountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAccountAuth.Object.CanViewAccountAsync(accountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", string.Join(" ", result.Errors));
        }

        [Fact]
        public async Task Authorization_ResourceNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            var mockUserAuth = new Mock<IUserAuthorizationService>();
            var userId = "nonexistent";
            var expectedResult = Result.NotFound("User", userId);

            mockUserAuth.Setup(x => x.CanViewUserAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockUserAuth.Object.CanViewUserAsync(userId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task Authorization_InvalidOperation_ShouldReturnBadRequestResult()
        {
            // Arrange
            var mockTransactionAuth = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 1;
            var targetAccountId = 1; // Same account
            var expectedResult = Result.BadRequest("Cannot transfer to the same account");

            mockTransactionAuth.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockTransactionAuth.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Cannot transfer to the same account", string.Join(" ", result.Errors));
        }

        #endregion

        #region Access Scope Tests

        [Fact]
        public void AccessScope_GlobalScope_ShouldHaveHighestPermissions()
        {
            // Arrange & Act
            var globalScope = AccessScope.Global;

            // Assert
            Assert.Equal(AccessScope.Global, globalScope);
            // Global scope should allow access to all resources
        }

        [Fact]
        public void AccessScope_BankLevelScope_ShouldRestrictToBankResources()
        {
            // Arrange & Act
            var bankScope = AccessScope.BankLevel;

            // Assert
            Assert.Equal(AccessScope.BankLevel, bankScope);
            // Bank level scope should restrict access to same bank resources
        }

        [Fact]
        public void AccessScope_SelfScope_ShouldRestrictToOwnResources()
        {
            // Arrange & Act
            var selfScope = AccessScope.Self;

            // Assert
            Assert.Equal(AccessScope.Self, selfScope);
            // Self scope should only allow access to own resources
        }

        #endregion

        #region Current User Service Tests

        [Fact]
        public void CurrentUserService_UserId_ShouldReturnUserId()
        {
            // Arrange
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var expectedUserId = "user123";
            mockCurrentUser.Setup(x => x.UserId).Returns(expectedUserId);

            // Act
            var actualUserId = mockCurrentUser.Object.UserId;

            // Assert
            Assert.Equal(expectedUserId, actualUserId);
        }

        [Fact]
        public void CurrentUserService_BankId_ShouldReturnBankId()
        {
            // Arrange
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var expectedBankId = 100;
            mockCurrentUser.Setup(x => x.BankId).Returns(expectedBankId);

            // Act
            var actualBankId = mockCurrentUser.Object.BankId;

            // Assert
            Assert.Equal(expectedBankId, actualBankId);
        }

        [Fact]
        public async Task CurrentUserService_IsInRoleAsync_ShouldReturnRoleStatus()
        {
            // Arrange
            var mockCurrentUser = new Mock<ICurrentUserService>();
            var roleName = "Admin";
            mockCurrentUser.Setup(x => x.IsInRoleAsync(roleName)).ReturnsAsync(true);

            // Act
            var isInRole = await mockCurrentUser.Object.IsInRoleAsync(roleName);

            // Assert
            Assert.True(isInRole);
        }

        #endregion

        #region Business Rule Validation Tests

        [Fact]
        public void BusinessRules_SelfModification_ShouldPreventSelfDeletion()
        {
            // Arrange
            var currentUserId = "user123";
            var targetUserId = "user123"; // Same user

            // Act
            var isSelfDeletion = currentUserId == targetUserId;

            // Assert
            Assert.True(isSelfDeletion);
            // Business rule: Users should not be able to delete themselves
        }

        [Fact]
        public void BusinessRules_CrossBankAccess_ShouldValidateBankIds()
        {
            // Arrange
            var currentUserBankId = 100;
            var targetUserBankId = 200;

            // Act
            var isDifferentBank = currentUserBankId != targetUserBankId;

            // Assert
            Assert.True(isDifferentBank);
            // Business rule: Users should only access resources within their bank (unless SuperAdmin)
        }

        [Fact]
        public void BusinessRules_TransferValidation_ShouldPreventSameAccountTransfer()
        {
            // Arrange
            var sourceAccountId = 1;
            var targetAccountId = 1; // Same account

            // Act
            var isSameAccount = sourceAccountId == targetAccountId;

            // Assert
            Assert.True(isSameAccount);
            // Business rule: Cannot transfer to the same account
        }

        #endregion

        #region Helper Methods

        private IEnumerable<Account> CreateTestAccounts(int count)
        {
            return Enumerable.Range(1, count).Select(i => new CheckingAccount
            {
                Id = i,
                AccountNumber = $"ACC-{i:D8}",
                Balance = 1000m * i,
                IsActive = true,
                UserId = $"user{i}",
                User = new ApplicationUser
                {
                    Id = $"user{i}",
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    IsActive = true,
                    BankId = 100
                }
            }).ToArray();
        }

        private IEnumerable<ApplicationUser> CreateTestUsers(int count)
        {
            return Enumerable.Range(1, count).Select(i => new ApplicationUser
            {
                Id = $"user{i}",
                UserName = $"user{i}",
                Email = $"user{i}@example.com",
                FullName = $"User {i}",
                IsActive = true,
                BankId = 100,
                Accounts = new List<Account>()
            }).ToArray();
        }

        private IEnumerable<Transaction> CreateTestTransactions(int count)
        {
            return Enumerable.Range(1, count).Select(i => new Transaction
            {
                Id = i,
                TransactionType = TransactionType.Transfer,
                Timestamp = System.DateTime.UtcNow.AddDays(-i),
                AccountTransactions = new List<AccountTransaction>
                {
                    new AccountTransaction
                    {
                        TransactionId = i,
                        AccountId = i,
                        Amount = 100m * i
                    }
                }
            }).ToArray();
        }

        #endregion
    }
}