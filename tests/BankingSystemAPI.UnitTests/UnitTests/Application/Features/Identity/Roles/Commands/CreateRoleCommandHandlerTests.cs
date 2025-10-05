#region Usings
using BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Application.Features.Identity.Roles.Commands;

/// <summary>
/// Tests for CreateRoleCommandHandler.
/// </summary>
public class CreateRoleCommandHandlerTests
{
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<ILogger<CreateRoleCommandHandler>> _mockLogger;
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _mockRoleService = new Mock<IRoleService>();
        _mockRoleManager = CreateMockRoleManager();
        _mockLogger = new Mock<ILogger<CreateRoleCommandHandler>>();
        _handler = new CreateRoleCommandHandler(
            _mockRoleService.Object,
            _mockRoleManager.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRoleName_ShouldSucceed()
    {
        // Arrange
        var command = new CreateRoleCommand("TestRole");
        var expectedResult = new RoleUpdateResultDto
        {
            Operation = "Create",
            Role = new RoleResDto { Id = "1", Name = "TestRole", Claims = new List<string>() }
        };

        _mockRoleManager.Setup(x => x.RoleExistsAsync("TestRole"))
            .ReturnsAsync(false);

        _mockRoleService.Setup(x => x.CreateRoleAsync(It.Is<RoleReqDto>(dto => dto.Name == "TestRole")))
            .ReturnsAsync(Result<RoleUpdateResultDto>.Success(expectedResult));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Create", result.Value.Operation);
        Assert.Equal("TestRole", result.Value.Role.Name);
    }

    [Fact]
    public async Task Handle_EmptyRoleName_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand(""); // Empty role name

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Role name cannot be null or empty.", result.Errors);
    }

    [Fact]
    public async Task Handle_NullRoleName_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand(null); // Null role name

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Role name cannot be null or empty.", result.Errors);
    }

    [Fact]
    public async Task Handle_ExistingRoleName_ShouldFail()
    {
        // Arrange
        var command = new CreateRoleCommand("ExistingRole");

        _mockRoleManager.Setup(x => x.RoleExistsAsync("ExistingRole"))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Role 'ExistingRole' already exists.", result.Errors);
    }

    [Theory]
    [InlineData("   ")] // Whitespace only
    [InlineData("\t")] // Tab only
    [InlineData("\n")] // Newline only
    public async Task Handle_WhitespaceRoleName_ShouldFail(string roleName)
    {
        // Arrange
        var command = new CreateRoleCommand(roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Role name cannot be null or empty.", result.Errors);
    }

    private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(
            store.Object, null, null, null, null);
    }
}
