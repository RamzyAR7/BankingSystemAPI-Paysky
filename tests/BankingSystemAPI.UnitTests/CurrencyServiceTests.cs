using AutoMapper;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.DTOs.Currency;
using Moq;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Exceptions;

namespace BankingSystemAPI.UnitTests
{
    public class CurrencyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CurrencyService _service;

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

            mapperMock.Setup(m => m.Map<IEnumerable<CurrencyDto>>(It.IsAny<IEnumerable<Currency>>() ))
                .Returns((IEnumerable<Currency> list) => list.Select(c => new CurrencyDto { Id = c.Id, Code = c.Code, ExchangeRate = c.ExchangeRate, IsBase = c.IsBase }));

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

            // create repositories directly
            var currencyRepo = new CurrencyRepository(_context);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _unitOfWork = new UnitOfWork(accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            _service = new CurrencyService(_unitOfWork, _mapper);
        }

        [Fact]
        public async Task Create_ReturnsCreatedCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = await _service.CreateAsync(req);
            Assert.NotNull(created);
            Assert.Equal("USD", created.Code);
            Assert.True(created.IsBase);
            Assert.True(created.Id > 0);
        }

        [Fact]
        public async Task GetAll_ReturnsCurrencies()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            await _service.CreateAsync(req);

            var all = (await _service.GetAllAsync()).ToList();
            Assert.Single(all);
            Assert.Equal("USD", all[0].Code);
        }

        [Fact]
        public async Task GetById_ReturnsCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = await _service.CreateAsync(req);

            var fetched = await _service.GetByIdAsync(created.Id);
            Assert.Equal(created.Code, fetched.Code);
        }

        [Fact]
        public async Task Update_ChangesExchangeRate()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = await _service.CreateAsync(req);

            var updateReq = new CurrencyReqDto { Code = "USD", ExchangeRate = 1.5m, IsBase = true };
            var updated = await _service.UpdateAsync(created.Id, updateReq);
            Assert.Equal(1.5m, updated.ExchangeRate);
        }

        [Fact]
        public async Task Delete_RemovesCurrency()
        {
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = await _service.CreateAsync(req);

            await _service.DeleteAsync(created.Id);
            await Assert.ThrowsAsync<CurrencyNotFoundException>(() => _service.GetByIdAsync(created.Id));
        }

        [Fact]
        public async Task Create_MissingCode_ThrowsBadRequest()
        {
            var req = new CurrencyReqDto { Code = "", ExchangeRate = 1m, IsBase = false };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(req));
        }

        [Fact]
        public async Task Create_InvalidExchangeRate_ThrowsBadRequest()
        {
            var req = new CurrencyReqDto { Code = "EUR", ExchangeRate = 0m, IsBase = false };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(req));
        }

        [Fact]
        public async Task Update_InvalidId_ThrowsBadRequest()
        {
            var req = new CurrencyReqDto { Code = "X", ExchangeRate = 1m, IsBase = false };
            await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync(0, req));
        }

        [Fact]
        public async Task Delete_InUse_ThrowsInvalidAccountOperation()
        {
            // create currency
            var req = new CurrencyReqDto { Code = "USD", ExchangeRate = 1m, IsBase = true };
            var created = await _service.CreateAsync(req);

            // add an account that uses this currency
            var user = new ApplicationUser { UserName = "accuser", Email = "acc@example.com", PhoneNumber = "6000000001", FullName = "Acc User", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            user.Id = Guid.NewGuid().ToString();
            _context.Users.Add(user);
            _context.SaveChanges();

            var acc = new CheckingAccount { AccountNumber = "X1", Balance = 0m, UserId = user.Id, CurrencyId = created.Id, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(acc);
            _context.SaveChanges();

            await Assert.ThrowsAsync<InvalidAccountOperationException>(() => _service.DeleteAsync(created.Id));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
