using BankingSystemAPI.Application.AuthorizationServices;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.UserSpecifications;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    /// <summary>
    /// Tests specifically for Admin role restrictions to ensure Admins can only view Client users
    /// </summary>
    public class AdminRoleRestrictionTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IScopeResolver> _mockScopeResolver;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<ILogger<UserAuthorizationService>> _mockLogger;
        private readonly UserAuthorizationService _authorizationService;

        public AdminRoleRestrictionTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockScopeResolver = new Mock<IScopeResolver>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockLogger = new Mock<ILogger<UserAuthorizationService>>();

            _mockUnitOfWork.Setup(x => x.UserRepository).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.RoleRepository).Returns(_mockRoleRepository.Object);

            _authorizationService = new UserAuthorizationService(
                _mockCurrentUserService.Object,
                _mockUnitOfWork.Object,
                _mockScopeResolver.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CanViewUserAsync_AdminViewingAdmin_ShouldReturnForbidden()
        {
            // Arrange - Simulating the real scenario from the user's request
            var adminUserId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Acting admin (alexjones)
            var targetAdminId = "b5451919-aea4-4bbc-9606-ef6e400a2f97"; // Target admin (janesmith)
            var bankId = 1;

            _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
            _mockCurrentUserService.Setup(x => x.BankId).Returns(bankId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // Setup target user as Admin
            var targetAdminUser = new ApplicationUser
            {
                Id = targetAdminId,
                UserName = "janesmith_updated",
                Email = "jane.smith@example.com",
                FullName = "Jane Smith",
                BankId = bankId,
                IsActive = true
            };

            _mockUserRepository
                .Setup(x => x.FindAsync(It.IsAny<UserByIdSpecification>()))
                .ReturnsAsync(targetAdminUser);

            // Setup role repository to return Admin role (not Client)
            var adminRole = new ApplicationRole { Id = "admin-role-id", Name = "Admin" };
            _mockRoleRepository
                .Setup(x => x.GetRoleByUserIdAsync(targetAdminId))
                .ReturnsAsync(adminRole);

            // Act
            var result = await _authorizationService.CanViewUserAsync(targetAdminId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("You can only access Client users.", result.Errors);
        }

        [Fact]
        public async Task CanViewUserAsync_AdminViewingClient_ShouldReturnSuccess()
        {
            // Arrange - Admin viewing a Client user (should be allowed)
            var adminUserId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Acting admin
            var targetClientId = "client-user-id"; // Target client
            var bankId = 1;

            _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
            _mockCurrentUserService.Setup(x => x.BankId).Returns(bankId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // Setup target user as Client
            var targetClientUser = new ApplicationUser
            {
                Id = targetClientId,
                UserName = "clientuser",
                Email = "client@example.com",
                FullName = "Client User",
                BankId = bankId,
                IsActive = true
            };

            _mockUserRepository
                .Setup(x => x.FindAsync(It.IsAny<UserByIdSpecification>()))
                .ReturnsAsync(targetClientUser);

            // Setup role repository to return Client role
            var clientRole = new ApplicationRole { Id = "client-role-id", Name = "Client" };
            _mockRoleRepository
                .Setup(x => x.GetRoleByUserIdAsync(targetClientId))
                .ReturnsAsync(clientRole);

            // Act
            var result = await _authorizationService.CanViewUserAsync(targetClientId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CanViewUserAsync_AdminViewingSuperAdmin_ShouldReturnForbidden()
        {
            // Arrange - Admin trying to view SuperAdmin (should be forbidden)
            var adminUserId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Acting admin
            var targetSuperAdminId = "superadmin-user-id"; // Target SuperAdmin
            var bankId = 1;

            _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
            _mockCurrentUserService.Setup(x => x.BankId).Returns(bankId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // Setup target user as SuperAdmin
            var targetSuperAdminUser = new ApplicationUser
            {
                Id = targetSuperAdminId,
                UserName = "superadminuser",
                Email = "superadmin@example.com",
                FullName = "Super Admin User",
                BankId = bankId,
                IsActive = true
            };

            _mockUserRepository
                .Setup(x => x.FindAsync(It.IsAny<UserByIdSpecification>()))
                .ReturnsAsync(targetSuperAdminUser);

            // Setup role repository to return SuperAdmin role
            var superAdminRole = new ApplicationRole { Id = "superadmin-role-id", Name = "SuperAdmin" };
            _mockRoleRepository
                .Setup(x => x.GetRoleByUserIdAsync(targetSuperAdminId))
                .ReturnsAsync(superAdminRole);

            // Act
            var result = await _authorizationService.CanViewUserAsync(targetSuperAdminId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("You can only access Client users.", result.Errors);
        }

        [Fact]
        public async Task CanViewUserAsync_AdminFromDifferentBank_ShouldReturnForbidden()
        {
            // Arrange - Admin trying to view user from different bank (should be forbidden)
            var adminUserId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Acting admin
            var targetClientId = "client-different-bank-id"; // Target client from different bank
            var adminBankId = 1;
            var targetBankId = 2; // Different bank

            _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
            _mockCurrentUserService.Setup(x => x.BankId).Returns(adminBankId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // Setup target user as Client from different bank
            var targetClientUser = new ApplicationUser
            {
                Id = targetClientId,
                UserName = "clientuser",
                Email = "client@example.com",
                FullName = "Client User",
                BankId = targetBankId, // Different bank ID
                IsActive = true
            };

            _mockUserRepository
                .Setup(x => x.FindAsync(It.IsAny<UserByIdSpecification>()))
                .ReturnsAsync(targetClientUser);

            // Setup role repository to return Client role
            var clientRole = new ApplicationRole { Id = "client-role-id", Name = "Client" };
            _mockRoleRepository
                .Setup(x => x.GetRoleByUserIdAsync(targetClientId))
                .ReturnsAsync(clientRole);

            // Act
            var result = await _authorizationService.CanViewUserAsync(targetClientId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Access forbidden due to bank isolation policy.", result.Errors);
        }

        [Fact]
        public async Task CanViewUserAsync_AdminViewingOwnProfile_ShouldReturnSuccess()
        {
            // Arrange - Admin viewing their own profile (self-access should always be allowed)
            var adminUserId = "19a16d6c-78dc-47de-8740-9c80f8cc1b90"; // Acting admin
            var bankId = 1;

            _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
            _mockCurrentUserService.Setup(x => x.BankId).Returns(bankId);
            _mockScopeResolver.Setup(x => x.GetScopeAsync()).ReturnsAsync(AccessScope.BankLevel);

            // Act - Admin trying to view themselves
            var result = await _authorizationService.CanViewUserAsync(adminUserId);

            // Assert - Should succeed regardless of role restrictions
            Assert.True(result.IsSuccess);
        }
    }
}