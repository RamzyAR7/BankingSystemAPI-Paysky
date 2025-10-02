using AutoMapper;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.DTOs.Currency;
using Moq;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetAllCurrencies;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetCurrencyById;
using System.Linq;

namespace BankingSystemAPI.UnitTests
{
    public class CurrencyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        // Handlers
        private readonly CreateCurrencyCommandHandler _createHandler;
        private readonly UpdateCurrencyCommandHandler _updateHandler;
        private readonly DeleteCurrencyCommandHandler _deleteHandler;
        private readonly GetAllCurrenciesQueryHandler _getAllHandler;
        private readonly GetCurrencyByIdQueryHandler _getByIdHandler;

        public CurrencyServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<Currency>(It.IsAny<CurrencyReqDto>()))
                .Returns((CurrencyReqDto req) => new Currency { Code = req.Code, ExchangeRate = req.ExchangeRate, IsBase = req.IsBase });

            mapperMock.Setup(m => m.Map<CurrencyDto>(It.IsAny<Currency>()))
                .Returns((Currency c) => new CurrencyDto { Id = c.Id, Code = c.Code, ExchangeRate = c.ExchangeRate, IsBase = c.IsBase });

            // handle both IEnumerable and List mapping expectations
            mapperMock.Setup(m => m.Map<IEnumerable<CurrencyDto>>(It.IsAny<IEnumerable<Currency>>() ))
                .Returns((IEnumerable<Currency> list) => list.Select(c => new CurrencyDto { Id = c.Id, Code = c.Code, ExchangeRate = c.ExchangeRate, IsBase = c.IsBase }));

            mapperMock.Setup(m => m.Map<List<CurrencyDto>>(It.IsAny<IEnumerable<Currency>>() ))
                .Returns((IEnumerable<Currency> list) => list.Select(c => new CurrencyDto { Id = c.Id, Code = c.Code, ExchangeRate = c.ExchangeRate, IsBase = c.IsBase }).ToList());

            // handle mapping from reqDto onto existing Currency instance for UpdateAsync
            mapperMock.Setup(m => m.Map(It.IsAny<CurrencyReqDto>(), It.IsAny<Currency>()))
                .Returns((CurrencyReqDto req, Currency existing) =>
                {
                    existing.Code = req.Code;
                    existing.ExchangeRate = req.ExchangeRate;
                    existing.IsBase = req.IsBase;
                    return existing;
                });

            _mapper = mapperMock.Object;

            // create cache service for repositories that require caching
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Cache.MemoryCacheService(memoryCache);

            // create repositories directly
            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _unitOfWork = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            // init handlers
            _createHandler = new CreateCurrencyCommandHandler(_unitOfWork, _mapper);
            _updateHandler = new UpdateCurrencyCommandHandler(_unitOfWork, _mapper);
            _deleteHandler = new DeleteCurrencyCommandHandler(_unitOfWork);
            _getAllHandler = new GetAllCurrenciesQueryHandler(_unitOfWork, _mapper);
            _getByIdHandler = new GetCurrencyByIdQueryHandler(_unitOfWork, _mapper);
        }

        [Fact]
        public async Task Create_ReturnsCreatedCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var result = await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None);
            Assert.True(result.Succeeded);
            var created = result.Value!;
            Assert.NotNull(created);
            Assert.Equal("USD", created.Code);
            Assert.True(created.IsBase);
            Assert.True(created.Id > 0);
        }

        [Fact]
        public async Task Create_MultipleBaseCurrencies_Fails()
        {
            var req1 = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var r1 = await _createHandler.Handle(new CreateCurrencyCommand(req1), CancellationToken.None);
            Assert.True(r1.Succeeded);

            var req2 = new CurrencyReqDto { Code = "EUR", ExchangeRate = 0.9m, IsBase = true };
            var r2 = await _createHandler.Handle(new CreateCurrencyCommand(req2), CancellationToken.None);
            Assert.False(r2.Succeeded);
            Assert.Contains(r2.Errors, e => e.Contains("base currency"));
        }

        [Fact]
        public async Task Update_SetBase_WhenAnotherBaseExists_Fails()
        {
            var baseReq = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var baseCurr = (await _createHandler.Handle(new CreateCurrencyCommand(baseReq), CancellationToken.None)).Value!;

            var otherReq = new CurrencyReqDto { Code = "EUR", ExchangeRate = 0.9m, IsBase = false };
            var other = (await _createHandler.Handle(new CreateCurrencyCommand(otherReq), CancellationToken.None)).Value!;

            var updateReq = new CurrencyReqDto { Code = "EUR", ExchangeRate = 0.9m, IsBase = true };
            var updateResult = await _updateHandler.Handle(new UpdateCurrencyCommand(other.Id, updateReq), CancellationToken.None);
            Assert.False(updateResult.Succeeded);
            Assert.Contains(updateResult.Errors, e => e.Contains("Another base currency"));
        }

        [Fact]
        public async Task GetAll_ReturnsCurrencies()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None);

            var all = (await _getAllHandler.Handle(new GetAllCurrenciesQuery(), CancellationToken.None)).Value!;
            Assert.Single(all);
            Assert.Equal("USD", all.First().Code);
        }

        [Fact]
        public async Task GetById_ReturnsCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = (await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None)).Value!;

            var fetched = (await _getByIdHandler.Handle(new GetCurrencyByIdQuery(created.Id), CancellationToken.None)).Value!;
            Assert.Equal(created.Code, fetched.Code);
        }

        [Fact]
        public async Task Update_ChangesExchangeRate()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = (await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None)).Value!;

            var updateReq = new CurrencyReqDto { Code = "USD", ExchangeRate = 1.5m, IsBase = true };
            var updated = (await _updateHandler.Handle(new UpdateCurrencyCommand(created.Id, updateReq), CancellationToken.None)).Value!;
            Assert.Equal(1.5m, updated.ExchangeRate);
        }

        [Fact]
        public async Task Delete_RemovesCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = (await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None)).Value!;

            var del = await _deleteHandler.Handle(new DeleteCurrencyCommand(created.Id), CancellationToken.None);
            Assert.True(del.Succeeded);

            var get = await _getByIdHandler.Handle(new GetCurrencyByIdQuery(created.Id), CancellationToken.None);
            Assert.False(get.Succeeded);
        }

        [Fact]
        public async Task Create_MissingCode_Fails()
        {
            var req = new CurrencyReqDto { Code = "", ExchangeRate = 1m, IsBase = false };
            var result = await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None);
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains("code"));
        }

        [Fact]
        public async Task Create_InvalidExchangeRate_Fails()
        {
            var req = new CurrencyReqDto { Code = "EUR", ExchangeRate = 0m, IsBase = false };
            var result = await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None);
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains("Exchange rate"));
        }

        [Fact]
        public async Task Update_InvalidId_Fails()
        {
            var req = new CurrencyReqDto { Code = "X", ExchangeRate = 1m, IsBase = false };
            var result = await _updateHandler.Handle(new UpdateCurrencyCommand(0, req), CancellationToken.None);
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task Delete_InUse_Fails()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = (await _createHandler.Handle(new CreateCurrencyCommand(req), CancellationToken.None)).Value!;

            // add an account that uses this currency
            var user = new ApplicationUser { UserName = "accuser", Email = "acc@example.com", PhoneNumber = "6000000001", FullName = "Acc User", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            user.Id = Guid.NewGuid().ToString();
            _context.Users.Add(user);
            _context.SaveChanges();

            var acc = new CheckingAccount { AccountNumber = "X1", Balance = 0m, UserId = user.Id, CurrencyId = created.Id, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(acc);
            _context.SaveChanges();

            var del = await _deleteHandler.Handle(new DeleteCurrencyCommand(created.Id), CancellationToken.None);
            Assert.False(del.Succeeded);
            Assert.Contains(del.Errors, e => e.Contains("in use"));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
