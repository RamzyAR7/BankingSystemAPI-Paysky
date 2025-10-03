using BankingSystemAPI.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims
{
    public class UpdateRoleClaimsCommandHandlerTests
    {
        private readonly Mock<IRoleClaimsService> _mockRoleClaimsService;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly UpdateRoleClaimsCommandHandler _handler;

        public UpdateRoleClaimsCommandHandlerTests()
        {
            _mockRoleClaimsService = new Mock<IRoleClaimsService>();
            _mockRoleManager = CreateMockRoleManager();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _handler = new UpdateRoleClaimsCommandHandler(
                _mockRoleClaimsService.Object,
                _mockRoleManager.Object,
                _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task Handle_ValidRoleAndClaims_ShouldSucceed()
        {
            // Arrange
            var roleId = "role-123";
            var claims = new List<string> { "Permission.User.Create", "Permission.User.Read" };
            var command = new UpdateRoleClaimsCommand(roleId, claims);

            var role = new ApplicationRole { Id = roleId, Name = "TestRole" };
            var expectedResult = new RoleClaimsUpdateResultDto
            {
                RoleName = "TestRole",
                UpdatedClaims = claims
            };

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            SetupHttpContextWithRole(UserRole.SuperAdmin.ToString());

            var updateDto = new UpdateRoleClaimsDto
            {
                RoleName = role.Name,
                Claims = claims
            };

            _mockRoleClaimsService.Setup(x => x.UpdateRoleClaimsAsync(It.IsAny<UpdateRoleClaimsDto>()))
                .ReturnsAsync(Result<RoleClaimsUpdateResultDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("TestRole", result.Value.RoleName);
            Assert.Equal(claims, result.Value.UpdatedClaims);
        }

        [Fact]
        public async Task Handle_RoleNotFound_ShouldFail()
        {
            // Arrange
            var roleId = "nonexistent-role";
            var claims = new List<string> { "Permission.User.Create" };
            var command = new UpdateRoleClaimsCommand(roleId, claims);

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync((ApplicationRole)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains($"Role with ID '{roleId}' not found.", result.Errors);
        }

        [Fact]
        public async Task Handle_SuperAdminRole_ShouldFail()
        {
            // Arrange
            var roleId = "role-123";
            var claims = new List<string> { "Permission.User.Create" };
            var command = new UpdateRoleClaimsCommand(roleId, claims);

            var role = new ApplicationRole { Id = roleId, Name = UserRole.SuperAdmin.ToString() };

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Cannot modify claims for SuperAdmin role", result.Errors);
        }

        [Fact]
        public async Task Handle_ClientRoleByNonSuperAdmin_ShouldFail()
        {
            // Arrange
            var roleId = "role-123";
            var claims = new List<string> { "Permission.User.Create" };
            var command = new UpdateRoleClaimsCommand(roleId, claims);

            var role = new ApplicationRole { Id = roleId, Name = UserRole.Client.ToString() };

            _mockRoleManager.Setup(x => x.FindByIdAsync(roleId))
                .ReturnsAsync(role);

            SetupHttpContextWithRole("Admin"); // Not SuperAdmin

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Only SuperAdmin can modify claims for Client role", result.Errors);
        }

        private void SetupHttpContextWithRole(string roleName)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, roleName) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(x => x.User).Returns(principal);

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        }

        private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
        {
            var store = new Mock<IRoleStore<ApplicationRole>>();
            return new Mock<RoleManager<ApplicationRole>>(
                store.Object, null, null, null, null);
        }
    }
}