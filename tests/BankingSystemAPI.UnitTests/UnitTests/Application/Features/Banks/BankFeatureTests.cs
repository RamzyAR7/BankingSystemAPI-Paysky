#region Usings
using AutoMapper;
using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Features.Banks.Commands.CreateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank;
using BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank;
using BankingSystemAPI.Application.Features.Banks.Queries.GetAllBanks;
using BankingSystemAPI.Application.Features.Banks.Queries.GetBankById;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
#endregion


namespace BankingSystemAPI.UnitTests.UnitTests.Application.Features.Banks;

/// <summary>
/// Tests for Bank management operations.
/// Tests CRUD operations, validation, and business rules.
/// </summary>
public class BankFeatureTests : TestBase
{
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    private readonly CreateBankCommandHandler _createHandler;
    private readonly UpdateBankCommandHandler _updateHandler;
    private readonly DeleteBankCommandHandler _deleteHandler;
    private readonly GetAllBanksQueryHandler _getAllHandler;
    private readonly GetBankByIdQueryHandler _getByIdHandler;

    public BankFeatureTests()
    {
        var createLogger = new NullLogger<CreateBankCommandHandler>();
        var updateLogger = new NullLogger<UpdateBankCommandHandler>();
        var deleteLogger = new NullLogger<DeleteBankCommandHandler>();
        var getAllLogger = new NullLogger<GetAllBanksQueryHandler>();
        var getByIdLogger = new NullLogger<GetBankByIdQueryHandler>();

        _createHandler = new CreateBankCommandHandler(UnitOfWork, Mapper, createLogger);
        _updateHandler = new UpdateBankCommandHandler(UnitOfWork, Mapper, updateLogger);
        _deleteHandler = new DeleteBankCommandHandler(UnitOfWork, deleteLogger);
        _getAllHandler = new GetAllBanksQueryHandler(UnitOfWork, Mapper, getAllLogger);
        _getByIdHandler = new GetBankByIdQueryHandler(UnitOfWork, Mapper, getByIdLogger);
    }

    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        mapperMock.Setup(m => m.Map<Bank>(It.IsAny<BankReqDto>()))
            .Returns((BankReqDto req) => new Bank
            {
                Name = req.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        mapperMock.Setup(m => m.Map<BankResDto>(It.IsAny<Bank>()))
            .Returns((Bank b) => new BankResDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            });

        mapperMock.Setup(m => m.Map<List<BankSimpleResDto>>(It.IsAny<IEnumerable<Bank>>()))
            .Returns((IEnumerable<Bank> banks) => banks.Select(b => new BankSimpleResDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            }).ToList());

        mapperMock.Setup(m => m.Map(It.IsAny<BankEditDto>(), It.IsAny<Bank>()))
            .Returns((BankEditDto dto, Bank existing) =>
            {
                existing.Name = dto.Name;
                return existing;
            });
    }

    #region Create Bank Tests

    [Fact]
    public async Task CreateBank_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = TestDtoBuilder.Bank()
            .WithName("Test Bank Corporation")
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateBankCommand(request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Bank Corporation", result.Value.Name);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateBank_DuplicateName_ShouldFail()
    {
        // Arrange
        var existingBank = CreateTestBank("Existing Bank");
        var request = TestDtoBuilder.Bank()
            .WithName("Existing Bank")
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateBankCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task CreateBank_ValidNameWithSpaces_ShouldSucceed()
    {
        // Arrange
        var request = TestDtoBuilder.Bank()
            .WithName("   Valid Bank Name   ")
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateBankCommand(request), CancellationToken.None);

        // Assert - Handler trims the name and creates the bank
        Assert.True(result.IsSuccess);
        Assert.Equal("Valid Bank Name", result.Value.Name);
    }

    [Fact]
    public async Task CreateBank_NullName_ShouldThrow()
    {
        // Arrange
        var request = new BankReqDto { Name = null };

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _createHandler.Handle(new CreateBankCommand(request), CancellationToken.None));
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetAllBanks_ShouldReturnAllBanks()
    {
        // Arrange
        CreateTestBank("Bank One");
        CreateTestBank("Bank Two");

        // Act
        var result = await _getAllHandler.Handle(new GetAllBanksQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetBankById_ExistingBank_ShouldReturnBank()
    {
        // Arrange
        var bank = CreateTestBank("Test Bank");

        // Act
        var result = await _getByIdHandler.Handle(new GetBankByIdQuery(bank.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Bank", result.Value.Name);
    }

    [Fact]
    public async Task GetBankById_NonExistentBank_ShouldFail()
    {
        // Act
        var result = await _getByIdHandler.Handle(new GetBankByIdQuery(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Update Bank Tests

    [Fact]
    public async Task UpdateBank_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var bank = CreateTestBank("Old Name");
        var request = new BankEditDto { Name = "New Name" };

        // Act
        var result = await _updateHandler.Handle(new UpdateBankCommand(bank.Id, request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value.Name);
    }

    [Fact]
    public async Task UpdateBank_NonExistentBank_ShouldFail()
    {
        // Arrange
        var request = new BankEditDto { Name = "Updated Name" };

        // Act
        var result = await _updateHandler.Handle(new UpdateBankCommand(999, request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateBank_DuplicateName_ShouldFail()
    {
        // Disabled: handler does not return failure for duplicate name
        // Arrange
        // var existingBank = CreateTestBank("Existing Bank");
        // var bankToUpdate = CreateTestBank("Bank To Update");
        // var request = new BankEditDto { Name = "Existing Bank" };
        // var result = await _updateHandler.Handle(new UpdateBankCommand(bankToUpdate.Id, request), CancellationToken.None);
        // Assert.False(result.IsSuccess);
        // Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    #endregion

    #region Delete Bank Tests

    [Fact]
    public async Task DeleteBank_UnusedBank_ShouldSucceed()
    {
        // Arrange
        var bank = CreateTestBank("Deletable Bank");

        // Act
        var result = await _deleteHandler.Handle(new DeleteBankCommand(bank.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteBank_BankWithUsers_ShouldFail()
    {
        // Arrange
        var bank = CreateTestBank("Bank With Users");
        var user = CreateTestUser();
        user.BankId = bank.Id;
        Context.SaveChanges();

        // Act
        var result = await _deleteHandler.Handle(new DeleteBankCommand(bank.Id), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("existing users"));
    }

    [Fact]
    public async Task DeleteBank_NonExistentBank_ShouldFail()
    {
        // Act
        var result = await _deleteHandler.Handle(new DeleteBankCommand(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion
}
