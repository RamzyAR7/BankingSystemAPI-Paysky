using BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Features.Identity.Roles.Commands.DeleteRole
{
    public class DeleteRoleCommandHandlerTests
    {
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ILogger<DeleteRoleCommandHandler>> _mockLogger;
        private readonly DeleteRoleCommandHandler _handler;

        public DeleteRoleCommandHandlerTests()
        {
            _mockRoleService = new Mock<IRoleService>();
            _mockLogger = new Mock<ILogger<DeleteRoleCommandHandler>>();
            _handler = new DeleteRoleCommandHandler(_mockRoleService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidRoleId_ShouldSucceed()
        {
            // Arrange
            var roleId = "role-123";
            var command = new DeleteRoleCommand(roleId);
            var expectedResult = new RoleUpdateResultDto
            {
                Operation = "Delete",
                Role = new RoleResDto { Id = roleId, Name = "TestRole", Claims = new List<string>() }
            };

            _mockRoleService.Setup(x => x.IsRoleInUseAsync(roleId))
                .ReturnsAsync(Result<bool>.Success(false));

            _mockRoleService.Setup(x => x.DeleteRoleAsync(roleId))
                .ReturnsAsync(Result<RoleUpdateResultDto>.Success(expectedResult));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Delete", result.Value.Operation);
            Assert.Equal(roleId, result.Value.Role.Id);
        }

        [Fact]
        public async Task Handle_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var command = new DeleteRoleCommand("");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role ID cannot be null or empty.", result.Errors);
        }

        [Fact]
        public async Task Handle_NullRoleId_ShouldFail()
        {
            // Arrange
            var command = new DeleteRoleCommand(null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role ID cannot be null or empty.", result.Errors);
        }

        [Fact]
        public async Task Handle_RoleInUse_ShouldFail()
        {
            // Arrange
            var roleId = "role-123";
            var command = new DeleteRoleCommand(roleId);

            _mockRoleService.Setup(x => x.IsRoleInUseAsync(roleId))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Cannot delete role because it is assigned to one or more users. Remove the role from all users before deletion.", result.Errors);
        }

        [Fact]
        public async Task Handle_ServiceFailure_ShouldReturnFailure()
        {
            // Arrange
            var roleId = "role-123";
            var command = new DeleteRoleCommand(roleId);

            _mockRoleService.Setup(x => x.IsRoleInUseAsync(roleId))
                .ReturnsAsync(Result<bool>.Success(false));

            _mockRoleService.Setup(x => x.DeleteRoleAsync(roleId))
                .ReturnsAsync(Result<RoleUpdateResultDto>.Failure("Role not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role not found", result.Errors);
        }

        [Theory]
        [InlineData("   ")] // Whitespace only
        [InlineData("\t")] // Tab only
        [InlineData("\n")] // Newline only
        public async Task Handle_WhitespaceRoleId_ShouldFail(string roleId)
        {
            // Arrange
            var command = new DeleteRoleCommand(roleId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Role ID cannot be null or empty.", result.Errors);
        }
    }
}