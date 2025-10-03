using AutoMapper;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetAllCurrencies;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetCurrencyById;
using BankingSystemAPI.UnitTests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BankingSystemAPI.UnitTests.Features.Currencies;

/// <summary>
/// Comprehensive tests for Currency CRUD operations.
/// Tests business rules, validation, and edge cases.
/// </summary>
public class CurrencyServiceTests : TestBase
{
    private readonly CreateCurrencyCommandHandler _createHandler;
    private readonly UpdateCurrencyCommandHandler _updateHandler;
    private readonly DeleteCurrencyCommandHandler _deleteHandler;
    private readonly GetAllCurrenciesQueryHandler _getAllHandler;
    private readonly GetCurrencyByIdQueryHandler _getByIdHandler;

    public CurrencyServiceTests()
    {
        var createLogger = new NullLogger<CreateCurrencyCommandHandler>();
        var getByIdLogger = new NullLogger<GetCurrencyByIdQueryHandler>();

        _createHandler = new CreateCurrencyCommandHandler(UnitOfWork, Mapper, createLogger);
        _updateHandler = new UpdateCurrencyCommandHandler(UnitOfWork, Mapper);
        _deleteHandler = new DeleteCurrencyCommandHandler(UnitOfWork);
        _getAllHandler = new GetAllCurrenciesQueryHandler(UnitOfWork, Mapper);
        _getByIdHandler = new GetCurrencyByIdQueryHandler(UnitOfWork, Mapper, getByIdLogger);
    }

    protected override void ConfigureMapperMock(Mock<IMapper> mapperMock)
    {
        mapperMock.Setup(m => m.Map<Currency>(It.IsAny<CurrencyReqDto>()))
            .Returns((CurrencyReqDto req) => new Currency 
            { 
                Code = req.Code, 
                ExchangeRate = req.ExchangeRate, 
                IsBase = req.IsBase 
            });

        mapperMock.Setup(m => m.Map<CurrencyDto>(It.IsAny<Currency>()))
            .Returns((Currency c) => new CurrencyDto 
            { 
                Id = c.Id, 
                Code = c.Code, 
                ExchangeRate = c.ExchangeRate, 
                IsBase = c.IsBase 
            });

        mapperMock.Setup(m => m.Map<List<CurrencyDto>>(It.IsAny<IEnumerable<Currency>>()))
            .Returns((IEnumerable<Currency> list) => list.Select(c => new CurrencyDto 
            { 
                Id = c.Id, 
                Code = c.Code, 
                ExchangeRate = c.ExchangeRate, 
                IsBase = c.IsBase 
            }).ToList());

        mapperMock.Setup(m => m.Map(It.IsAny<CurrencyReqDto>(), It.IsAny<Currency>()))
            .Returns((CurrencyReqDto req, Currency existing) =>
            {
                existing.Code = req.Code;
                existing.ExchangeRate = req.ExchangeRate;
                existing.IsBase = req.IsBase;
                return existing;
            });
    }

    #region Create Currency Tests

    [Fact]
    public async Task CreateCurrency_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = TestDtoBuilder.Currency()
            .WithCode("EUR")
            .WithRate(0.85m)
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateCurrencyCommand(request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("EUR", result.Value.Code);
        Assert.Equal(0.85m, result.Value.ExchangeRate);
        Assert.False(result.Value.IsBase);
    }

    [Fact]
    public async Task CreateCurrency_SecondBaseCurrency_ShouldFail()
    {
        // Arrange - First base currency already exists (seeded in TestBase)
        var request = TestDtoBuilder.Currency()
            .WithCode("EUR")
            .WithRate(0.85m)
            .AsBase()
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateCurrencyCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("base currency"));
    }

    [Fact]
    public async Task CreateCurrency_DuplicateCode_ShouldSucceed()
    {
        // Arrange - Create first currency
        await CreateTestCurrency("GBP", 1.25m, false);
        
        // Try to create another with same code
        var request = TestDtoBuilder.Currency()
            .WithCode("GBP")
            .WithRate(1.30m)
            .Build();

        // Act
        var result = await _createHandler.Handle(new CreateCurrencyCommand(request), CancellationToken.None);

        // Assert - The handler doesn't prevent duplicate codes, database constraints would handle this
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Update Currency Tests

    [Fact]
    public async Task UpdateCurrency_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var created = await CreateTestCurrency("GBP", 1.25m, false);
        var updateRequest = TestDtoBuilder.Currency()
            .WithCode("GBP")
            .WithRate(1.30m)
            .Build();

        // Act
        var result = await _updateHandler.Handle(new UpdateCurrencyCommand(created.Id, updateRequest), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1.30m, result.Value.ExchangeRate);
    }

    [Fact]
    public async Task UpdateCurrency_SetAsSecondBase_ShouldFail()
    {
        // Arrange
        var created = await CreateTestCurrency("GBP", 1.25m, false);
        var updateRequest = TestDtoBuilder.Currency()
            .WithCode("GBP")
            .WithRate(1.25m)
            .AsBase()
            .Build();

        // Act
        var result = await _updateHandler.Handle(new UpdateCurrencyCommand(created.Id, updateRequest), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Another base currency"));
    }

    #endregion

    #region Delete Currency Tests

    [Fact]
    public async Task DeleteCurrency_UnusedCurrency_ShouldSucceed()
    {
        // Arrange
        var currency = await CreateTestCurrency("JPY", 110m, false);

        // Act
        var result = await _deleteHandler.Handle(new DeleteCurrencyCommand(currency.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteCurrency_UsedByAccount_ShouldFail()
    {
        // Arrange
        var currency = await CreateTestCurrency("CAD", 1.35m, false);
        var user = CreateTestUser();
        var account = TestEntityFactory.CreateCheckingAccount(user.Id, currency.Id);
        Context.CheckingAccounts.Add(account);
        Context.SaveChanges();

        // Act
        var result = await _deleteHandler.Handle(new DeleteCurrencyCommand(currency.Id), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("in use"));
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetAllCurrencies_ShouldReturnAllCurrencies()
    {
        // Arrange
        await CreateTestCurrency("EUR", 0.85m, false);
        await CreateTestCurrency("GBP", 1.25m, false);

        // Act
        var result = await _getAllHandler.Handle(new GetAllCurrenciesQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count); // Base USD + 2 created
    }

    [Fact]
    public async Task GetCurrencyById_ExistingCurrency_ShouldReturnCurrency()
    {
        // Arrange
        var currency = await CreateTestCurrency("AUD", 1.45m, false);

        // Act
        var result = await _getByIdHandler.Handle(new GetCurrencyByIdQuery(currency.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("AUD", result.Value.Code);
        Assert.Equal(1.45m, result.Value.ExchangeRate);
    }

    [Fact]
    public async Task GetCurrencyById_NonExistentCurrency_ShouldFail()
    {
        // Act
        var result = await _getByIdHandler.Handle(new GetCurrencyByIdQuery(999), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Helper Methods

    private async Task<Currency> CreateTestCurrency(string code, decimal rate, bool isBase)
    {
        var request = TestDtoBuilder.Currency()
            .WithCode(code)
            .WithRate(rate);

        if (isBase) request.AsBase();

        var result = await _createHandler.Handle(new CreateCurrencyCommand(request.Build()), CancellationToken.None);
        Assert.True(result.IsSuccess);

        return Context.Currencies.First(c => c.Code == code);
    }

    #endregion
}
