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
    /// Tests to verify that account and transaction authorization services 
    /// properly enforce access control and prevent unauthorized operations.
    /// </summary>
    public class AccountTransactionAuthorizationTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<ILogger<AccountAuthorizationService>> _mockAccountLogger;
        private readonly Mock<ILogger<TransactionAuthorizationService>> _mockTransactionLogger;
        private readonly AccountAuthorizationService _accountAuthService;
        private readonly TransactionAuthorizationService _transactionAuthService;

        public AccountTransactionAuthorizationTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockAccountLogger = new Mock<ILogger<AccountAuthorizationService>>();
            _mockTransactionLogger = new Mock<ILogger<TransactionAuthorizationService>>();

            _accountAuthService = new AccountAuthorizationService(
                _mockCurrentUserService.Object,
                _mockUnitOfWork.Object,
                _mockScopeResolver.Object,
                _mockAccountLogger.Object);

            _transactionAuthService = new TransactionAuthorizationService(
                _mockCurrentUserService.Object,
                _mockUnitOfWork.Object,
                _mockScopeResolver.Object,
                _mockTransactionLogger.Object);
        }

        #region Account Authorization Tests

        [Fact]
        public async Task CanModifyAccountAsync_SelfAccountEdit_ShouldReturnForbidden()
        {
            // Arrange
            var userId = "user123";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = userId,
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Edit);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Users cannot edit their own accounts.", result.Errors);
        }

        [Fact]
        public async Task CanModifyAccountAsync_SelfAccountDelete_ShouldReturnForbidden()
        {
            // Arrange
            var userId = "user123";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = userId,
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Delete);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Users cannot delete their own accounts.", result.Errors);
        }

        [Fact]
        public async Task CanModifyAccountAsync_SelfAccountDeposit_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user123";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = userId,
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Deposit);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanModifyAccountAsync_SelfAccountWithdraw_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user123";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Global);

            var mockUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = userId,
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Withdraw);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region Transaction Authorization Tests

        [Fact]
        public async Task CanInitiateTransferAsync_FromOwnAccount_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user123";
            var sourceAccountId = 1;
            var targetAccountId = 2;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);

            var mockUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            var mockSourceAccount = new CheckingAccount 
            { 
                Id = sourceAccountId, 
                UserId = userId,  // User owns the source account
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockUser
            };

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockSourceAccount);

            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _transactionAuthService.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanInitiateTransferAsync_FromOtherAccount_ShouldReturnForbidden()
        {
            // Arrange
            var userId = "user123";
            var otherUserId = "user456";
            var sourceAccountId = 1;
            var targetAccountId = 2;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);

            var mockOtherUser = new ApplicationUser { Id = otherUserId, UserName = "otheruser" };
            var mockSourceAccount = new CheckingAccount 
            { 
                Id = sourceAccountId, 
                UserId = otherUserId,  // User does NOT own the source account
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockOtherUser
            };

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockSourceAccount);

            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _transactionAuthService.CanInitiateTransferAsync(sourceAccountId, targetAccountId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Clients cannot initiate transfers from accounts they don't own.", result.Errors.First());
        }

        #endregion

        #region Authorization Scope Tests

        [Theory]
        [InlineData(AccessScope.Global)]
        public async Task CanModifyAccountAsync_AdminScopes_ShouldReturnSuccess(AccessScope scope)
        {
            // Arrange
            var currentUserId = "admin123";
            var targetUserId = "user456";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(currentUserId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(scope);

            var mockCurrentUser = new ApplicationUser { Id = currentUserId, UserName = "admin" };
            var mockTargetUser = new ApplicationUser { Id = targetUserId, UserName = "testuser" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = targetUserId,  // Different user's account
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockTargetUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockCurrentUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            var mockRoleRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IRoleRepository>();
            mockRoleRepository.Setup(x => x.GetRoleByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationRole { Name = "Client" });

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);
            _mockUnitOfWork.Setup(x => x.RoleRepository).Returns(mockRoleRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Deposit);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanModifyAccountAsync_SelfScope_OnOtherAccount_ShouldReturnForbidden()
        {
            // Arrange
            var currentUserId = "user123";
            var targetUserId = "user456";
            var accountId = 1;
            
            _mockCurrentUserService.Setup(x => x.UserId).Returns(currentUserId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.Self);

            var mockCurrentUser = new ApplicationUser { Id = currentUserId, UserName = "user1" };
            var mockTargetUser = new ApplicationUser { Id = targetUserId, UserName = "user2" };
            var mockAccount = new CheckingAccount 
            { 
                Id = accountId, 
                UserId = targetUserId,  // Different user's account
                AccountNumber = "ACC-001",
                Balance = 1000m,
                User = mockTargetUser
            };

            var mockUserRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IUserRepository>();
            mockUserRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.UserSpecifications.UserByIdSpecification>()))
                .ReturnsAsync(mockCurrentUser);

            var mockAccountRepository = new Mock<BankingSystemAPI.Application.Interfaces.Repositories.IAccountRepository>();
            mockAccountRepository.Setup(x => x.FindAsync(It.IsAny<BankingSystemAPI.Application.Specifications.AccountSpecification.AccountByIdSpecification>()))
                .ReturnsAsync(mockAccount);

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.AccountRepository).Returns(mockAccountRepository.Object);

            // Act
            var result = await _accountAuthService.CanModifyAccountAsync(accountId, AccountModificationOperation.Deposit);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Clients cannot modify other users' accounts.", result.Errors.First());
        }

        #endregion
    }
}