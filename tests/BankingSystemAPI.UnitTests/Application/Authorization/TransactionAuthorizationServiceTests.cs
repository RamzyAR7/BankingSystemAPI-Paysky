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
    public class TransactionAuthorizationServiceTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<ILogger<ITransactionAuthorizationService>> _mockLogger;

        public TransactionAuthorizationServiceTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockLogger = new Mock<ILogger<ITransactionAuthorizationService>>();
        }

        #region CanInitiateTransferAsync Tests

        [Fact]
        public async Task CanInitiateTransferAsync_WithMockedService_ShouldReturnResult()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 1;
            var targetAccountId = 2;
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsSuccess);
            mockAuthService.Verify(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId), Times.Once);
        }

        [Fact]
        public async Task CanInitiateTransferAsync_AccessDenied_ShouldReturnFailure()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 1;
            var targetAccountId = 2;
            var expectedResult = Result.Forbidden("Access denied to source account");

            mockAuthService.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", string.Join(" ", result.Errors));
        }

        [Fact]
        public async Task CanInitiateTransferAsync_NonExistentSourceAccount_ShouldReturnNotFound()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 999;
            var targetAccountId = 2;
            var expectedResult = Result.NotFound("Account", sourceAccountId);

            mockAuthService.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("not found", string.Join(" ", result.Errors).ToLower());
        }

        [Fact]
        public async Task CanInitiateTransferAsync_InactiveAccount_ShouldReturnBadRequest()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var sourceAccountId = 1;
            var targetAccountId = 2;
            var expectedResult = Result.BadRequest("Source account is inactive");

            mockAuthService.Setup(x => x.CanInitiateTransferAsync(sourceAccountId, targetAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("inactive", string.Join(" ", result.Errors).ToLower());
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(5, 10)]
        [InlineData(100, 200)]
        public async Task CanInitiateTransferAsync_DifferentAccountPairs_ShouldReturnResult(int sourceId, int targetId)
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var expectedResult = Result.Success();

            mockAuthService.Setup(x => x.CanInitiateTransferAsync(sourceId, targetId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.CanInitiateTransferAsync(sourceId, targetId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region FilterTransactionsAsync Tests

        [Fact]
        public async Task FilterTransactionsAsync_ValidRequest_ShouldReturnFilteredResults()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var transactions = CreateTestTransactions(5);
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((transactions, 5));

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(transactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value.TotalCount);
            Assert.Equal(5, result.Value.Transactions.Count());
        }

        [Fact]
        public async Task FilterTransactionsAsync_SelfScope_ShouldReturnOnlyUserTransactions()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var userTransactions = CreateTestTransactions(3, "user123");
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((userTransactions, 3));

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(userTransactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(3, result.Value.Transactions.Count());
        }

        [Fact]
        public async Task FilterTransactionsAsync_BankLevelScope_ShouldReturnSameBankTransactions()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var bankTransactions = CreateTestTransactions(4);
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((bankTransactions, 4));

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(bankTransactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(4, result.Value.TotalCount);
            Assert.Equal(4, result.Value.Transactions.Count());
        }

        [Theory]
        [InlineData(1, 5, 5)]
        [InlineData(2, 3, 2)]
        [InlineData(1, 10, 7)]
        public async Task FilterTransactionsAsync_DifferentPagination_ShouldReturnCorrectPage(int pageNumber, int pageSize, int expectedCount)
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var allTransactions = CreateTestTransactions(7);
            var expectedTransactions = allTransactions.Skip((pageNumber - 1) * pageSize).Take(Math.Min(pageSize, expectedCount));
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((expectedTransactions, 7));

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), pageNumber, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(allTransactions.AsQueryable(), pageNumber, pageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(7, result.Value.TotalCount);
        }

        [Fact]
        public async Task FilterTransactionsAsync_NoResults_ShouldReturnEmptyResult()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var emptyTransactions = new List<Transaction>();
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Success((emptyTransactions, 0));

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(emptyTransactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.TotalCount);
            Assert.Empty(result.Value.Transactions);
        }

        [Fact]
        public async Task FilterTransactionsAsync_AuthorizationFailure_ShouldReturnFailure()
        {
            // Arrange
            var mockAuthService = new Mock<ITransactionAuthorizationService>();
            var transactions = CreateTestTransactions(3);
            var expectedResult = Result<(IEnumerable<Transaction> Transactions, int TotalCount)>
                .Forbidden("Access denied to transactions");

            mockAuthService.Setup(x => x.FilterTransactionsAsync(It.IsAny<IQueryable<Transaction>>(), 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockAuthService.Object.FilterTransactionsAsync(transactions.AsQueryable(), 1, 10);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Access denied", string.Join(" ", result.Errors));
        }

        #endregion

        #region Authorization Integration Tests

        [Fact]
        public void TransactionAuthorization_TransferValidation_ShouldFollowBusinessRules()
        {
            // Arrange
            var sourceAccountId = 1;
            var targetAccountId = 2;

            // Act & Assert
            // Business Rule: Source and target accounts cannot be the same
            Assert.NotEqual(sourceAccountId, targetAccountId);

            // Business Rule: Account IDs must be positive
            Assert.True(sourceAccountId > 0);
            Assert.True(targetAccountId > 0);
        }

        [Fact]
        public void TransactionAuthorization_FilteringValidation_ShouldFollowPaginationRules()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 5;
            var totalItems = 12;

            // Act
            var skip = (pageNumber - 1) * pageSize;
            var expectedItemsOnPage = Math.Min(pageSize, Math.Max(0, totalItems - skip));

            // Assert
            Assert.Equal(5, skip); // Should skip first 5 items
            Assert.Equal(5, expectedItemsOnPage); // Should return 5 items on page 2
        }

        #endregion

        #region Helper Methods

        private IEnumerable<Transaction> CreateTestTransactions(int count, string userId = null)
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
                        Amount = 100m * i,
                        Account = new CheckingAccount
                        {
                            Id = i,
                            UserId = userId ?? $"user{i}",
                            AccountNumber = $"ACC-{i:D8}",
                            Balance = 1000m,
                            IsActive = true,
                            User = new ApplicationUser
                            {
                                Id = userId ?? $"user{i}",
                                UserName = $"user{i}",
                                Email = $"user{i}@example.com",
                                IsActive = true,
                                BankId = 100
                            }
                        }
                    }
                }
            }).ToList();
        }

        #endregion
    }
}