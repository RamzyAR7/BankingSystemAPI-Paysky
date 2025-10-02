using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Moq;
using AutoMapper;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.DTOs.Account;
using Xunit;
using System.Linq;
using System;
using System.Collections.Generic;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId;
using BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount;
using BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts;

namespace BankingSystemAPI.UnitTests
{
    public class AccountServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        // handlers
        private readonly GetAccountByIdQueryHandler _getByIdHandler;
        private readonly GetAccountByAccountNumberQueryHandler _getByNumberHandler;
        private readonly GetAccountsByUserIdQueryHandler _getByUserHandler;
        private readonly DeleteAccountCommandHandler _deleteHandler;
        private readonly DeleteAccountsCommandHandler _deleteManyHandler;

        public AccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // create cache service and repositories with explicit DI
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Cache.MemoryCacheService(memoryCache);

            var userRepo = new UserRepository(_context);
            var roleRepo = new RoleRepository(_context, cacheService);
            var currencyRepo = new CurrencyRepository(_context, cacheService);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);

            _uow = new UnitOfWork(userRepo, roleRepo, accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
                .Returns((Account a) => new AccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty, Type = a.GetType().Name });
            mapperMock.Setup(m => m.Map<IEnumerable<AccountDto>>(It.IsAny<IEnumerable<Account>>() ))
                .Returns((IEnumerable<Account> list) => list.Select(a => new AccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty, Type = a.GetType().Name }));

            _mapper = mapperMock.Object;

            // init handlers
            _getByIdHandler = new GetAccountByIdQueryHandler(_uow, _mapper);
            _getByNumberHandler = new GetAccountByAccountNumberQueryHandler(_uow, _mapper);
            _getByUserHandler = new GetAccountsByUserIdQueryHandler(_uow, _mapper);
            _deleteHandler = new DeleteAccountCommandHandler(_uow);
            _deleteManyHandler = new DeleteAccountsCommandHandler(_uow);

            // seed some accounts and users
            _context.Currencies.Add(new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true });
            _context.SaveChanges();

            var user = new ApplicationUser { UserName = "user1", Email = "u1@example.com", PhoneNumber = "2000000001", FullName = "User One", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            user.Id = Guid.NewGuid().ToString();
            _context.Users.Add(user);
            _context.SaveChanges();

            var acc = new CheckingAccount { AccountNumber = "A1", Balance = 0m, UserId = user.Id, CurrencyId = 1, RowVersion = new byte[8] };
            _context.CheckingAccounts.Add(acc);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAccountById_ReturnsDto()
        {
            var existing = _context.CheckingAccounts.First();
            var res = await _getByIdHandler.Handle(new GetAccountByIdQuery(existing.Id), CancellationToken.None);
            Assert.True(res.Succeeded);
            var dto = res.Value!;
            Assert.Equal(existing.AccountNumber, dto.AccountNumber);
        }

        [Fact]
        public async Task GetAccountById_InvalidId_ReturnsFailure()
        {
            var res = await _getByIdHandler.Handle(new GetAccountByIdQuery(0), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task GetByAccountNumber_ReturnsDto()
        {
            var existing = _context.CheckingAccounts.First();
            var res = await _getByNumberHandler.Handle(new GetAccountByAccountNumberQuery(existing.AccountNumber), CancellationToken.None);
            Assert.True(res.Succeeded);
            var dto = res.Value!;
            Assert.Equal(existing.AccountNumber, dto.AccountNumber);
        }

        [Fact]
        public async Task GetByAccountNumber_Invalid_ReturnsFailure()
        {
            var res = await _getByNumberHandler.Handle(new GetAccountByAccountNumberQuery("  "), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task GetAccountsByUserId_ReturnsList()
        {
            var user = _context.Users.First();
            var res = await _getByUserHandler.Handle(new GetAccountsByUserIdQuery(user.Id), CancellationToken.None);
            Assert.True(res.Succeeded);
            var list = res.Value!;
            Assert.Single(list);
            Assert.Equal(user.Id, list.First().UserId);
        }

        [Fact]
        public async Task GetAccountsByUserId_Invalid_ReturnsFailure()
        {
            var res = await _getByUserHandler.Handle(new GetAccountsByUserIdQuery(""), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task DeleteAccount_WithBalance_ReturnsFailure()
        {
            var acc = _context.CheckingAccounts.First();
            acc.Balance = 10m;
            _context.SaveChanges();

            var res = await _deleteHandler.Handle(new DeleteAccountCommand(acc.Id), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task DeleteMany_PartialMissing_ReturnsFailure()
        {
            var existing = _context.CheckingAccounts.First();
            var res = await _deleteManyHandler.Handle(new DeleteAccountsCommand(new[] { existing.Id, 999 }), CancellationToken.None);
            Assert.False(res.Succeeded);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
