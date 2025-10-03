using AutoMapper;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Infrastructure.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            _mockRoleManager = CreateMockRoleManager();
            _mockUserManager = CreateMockUserManager();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            _roleService = new RoleService(
                _mockRoleManager.Object,
                _mockUserManager.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateRoleAsync_ValidRole_ShouldSucceed()
        {
            // Arrange
            var roleDto = new RoleReqDto { Name = "TestRole" };
            var role = new ApplicationRole { Id = "1", Name = "TestRole" };
            var roleResDto = new RoleResDto { Id = "1", Name = "TestRole", Claims = new List<string>() };

            _mockRoleManager.Setup(x => x.FindByNameAsync("TestRole"))
                .ReturnsAsync((ApplicationRole)null);

            _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockMapper.Setup(x => x.Map<RoleResDto>(It.IsAny<ApplicationRole>()))
                .Returns(roleResDto);

            // Act
            var result = await _roleService.CreateRoleAsync(roleDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Create", result.Value.Operation);
            Assert.Equal("TestRole", result.Value.Role.Name);
        }

        [Fact]
        public async Task CreateRoleAsync_EmptyName_ShouldFail()
        {
            // Arrange
            var roleDto = new RoleReqDto { Name = "" };

            // Act
            var result = await _roleService.CreateRoleAsync(roleDto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role name cannot be null or empty.", result.Errors);
        }

        [Fact]
        public async Task CreateRoleAsync_NullName_ShouldFail()
        {
            // Arrange
            var roleDto = new RoleReqDto { Name = null };

            // Act
            var result = await _roleService.CreateRoleAsync(roleDto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role name cannot be null or empty.", result.Errors);
        }

        [Fact]
        public async Task CreateRoleAsync_DuplicateName_ShouldFail()
        {
            // Arrange
            var roleDto = new RoleReqDto { Name = "ExistingRole" };
            var existingRole = new ApplicationRole { Id = "1", Name = "ExistingRole" };

            _mockRoleManager.Setup(x => x.FindByNameAsync("ExistingRole"))
                .ReturnsAsync(existingRole);

            // Act
            var result = await _roleService.CreateRoleAsync(roleDto);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role 'ExistingRole' already exists.", result.Errors);
        }

        [Fact]
        public async Task DeleteRoleAsync_ValidRoleId_ShouldSucceed()
        {
            // Arrange
            var roleId = "role-123";
            var role = new ApplicationRole { Id = roleId, Name = "TestRole" };
            var roleResDto = new RoleResDto { Id = roleId, Name = "TestRole", Claims = new List<string>() };

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            _mockRoleManager.Setup(x => x.DeleteAsync(role))
                .ReturnsAsync(IdentityResult.Success);

            _mockMapper.Setup(x => x.Map<RoleResDto>(role))
                .Returns(roleResDto);

            // Act
            var result = await _roleService.DeleteRoleAsync(roleId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Delete", result.Value.Operation);
            Assert.Equal("TestRole", result.Value.Role.Name);
        }

        [Fact]
        public async Task DeleteRoleAsync_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var roleId = "";

            // Act
            var result = await _roleService.DeleteRoleAsync(roleId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role ID cannot be null or empty.", result.Errors);
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleNotFound_ShouldFail()
        {
            // Arrange
            var roleId = "nonexistent-role";

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync((ApplicationRole)null);

            // Act
            var result = await _roleService.DeleteRoleAsync(roleId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains($"Role with ID '{roleId}' not found.", result.Errors);
        }

        [Fact]
        public async Task GetAllRolesAsync_ShouldReturnAllRoles()
        {
            // Arrange
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole { Id = "1", Name = "Admin" },
                new ApplicationRole { Id = "2", Name = "User" }
            };

            var roleResDtos = new List<RoleResDto>
            {
                new RoleResDto { Id = "1", Name = "Admin", Claims = new List<string>() },
                new RoleResDto { Id = "2", Name = "User", Claims = new List<string>() }
            };

            _mockRoleManager.Setup(x => x.Roles)
                .Returns(roles.AsQueryable());

            _mockMapper.Setup(x => x.Map<List<RoleResDto>>(It.IsAny<List<ApplicationRole>>()))
                .Returns(roleResDtos);

            _mockRoleManager.Setup(x => x.FindByNameAsync("Admin"))
                .ReturnsAsync(roles[0]);
            _mockRoleManager.Setup(x => x.FindByNameAsync("User"))
                .ReturnsAsync(roles[1]);

            _mockRoleManager.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync((IList<Claim>)new List<Claim>());

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(result.Value, r => r.Name == "Admin");
            Assert.Contains(result.Value, r => r.Name == "User");
        }

        private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
        {
            var store = new Mock<IRoleStore<ApplicationRole>>();
            return new Mock<RoleManager<ApplicationRole>>(
                store.Object, null, null, null, null);
        }

        private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }
    }
}