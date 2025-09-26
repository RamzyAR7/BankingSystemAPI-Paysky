using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Services;
using Moq;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.DTOs.Account;
using AutoMapper;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using System.Collections.Generic;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.UnitTests
{
    public class CheckingAccountServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CheckingAccountService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly Mock<IAccountAuthorizationService> _accountAuthMock;

        public CheckingAccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // seed currency
            _context.Currencies.Add(new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true });
            _context.SaveChanges();

            var roleStore = new RoleStore<ApplicationRole>(_context);
            _roleManager = new RoleManager<ApplicationRole>(roleStore,
                new IRoleValidator<ApplicationRole>[] { new RoleValidator<ApplicationRole>() },
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new NullLogger<RoleManager<ApplicationRole>>());

            var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>(_context);
            _userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            _currentUserMock = new Mock<ICurrentUserService>();

            // create cache service and repositories with explicit DI
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cacheService = new BankingSystemAPI.Infrastructure.Services.MemoryCacheService(memoryCache);

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
            mapperMock.Setup(m => m.Map<CheckingAccountDto>(It.IsAny<CheckingAccount>()))
                .Returns((CheckingAccount a) => new CheckingAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty });
            mapperMock.Setup(m => m.Map<IEnumerable<CheckingAccountDto>>(It.IsAny<IEnumerable<CheckingAccount>>() ))
                .Returns((IEnumerable<CheckingAccount> list) => list.Select(a => new CheckingAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty }));
            mapperMock.Setup(m => m.Map<CheckingAccount>(It.IsAny<CheckingAccountReqDto>()))
                .Returns((CheckingAccountReqDto req) => new CheckingAccount { UserId = req.UserId, CurrencyId = req.CurrencyId, Balance = req.InitialBalance, RowVersion = new byte[8] });

            _mapper = mapperMock.Object;

            _accountAuthMock = new Mock<IAccountAuthorizationService>();
            _accountAuthMock.Setup(a => a.CanViewAccountAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _accountAuthMock.Setup(a => a.CanModifyAccountAsync(It.IsAny<int>(), It.IsAny<AccountModificationOperation>())).Returns(Task.CompletedTask);
            _accountAuthMock.Setup(a => a.FilterAccountsQueryAsync(It.IsAny<IQueryable<Account>>())).ReturnsAsync((IQueryable<Account> q) => q);
            _accountAuthMock.Setup(a => a.CanCreateAccountForUserAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            _service = new CheckingAccountService(_uow, _mapper, _accountAuthMock.Object);
        }

        [Fact]
        public async Task CreateAccount_Succeeds_WhenUserHasRole()
        {
            var user = new ApplicationUser { UserName = "cu1", Email = "cu1@example.com", PhoneNumber = "0000000003", FullName = "CU1", NationalId = "NID1", DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            await _userManager.CreateAsync(user, "Password123!");
            // ensure role exists via RoleManager
            if (!await _roleManager.RoleExistsAsync("Client")) { await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" }); }
            await _userManager.AddToRoleAsync(user, "Client");

            var req = new CheckingAccountReqDto { UserId = user.Id, CurrencyId = _context.Currencies.First().Id, InitialBalance = 10m };
            var res = await _service.CreateAccountAsync(req);

            Assert.NotNull(res);
            Assert.Equal(user.Id, res.UserId);
            Assert.Equal("USD", res.CurrencyCode);
        }

        [Fact]
        public async Task CreateAccount_Throws_WhenUserHasNoRole()
        {
            var user = new ApplicationUser { UserName = "cu2", Email = "cu2@example.com", PhoneNumber = "0000000004", FullName = "CU2", NationalId = "NID2", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
            await _userManager.CreateAsync(user, "Password123!");

            var req = new CheckingAccountReqDto { UserId = user.Id, CurrencyId = _context.Currencies.First().Id, InitialBalance = 0m };
            await Assert.ThrowsAsync<BankingSystemAPI.Application.Exceptions.BadRequestException>(() => _service.CreateAccountAsync(req));
        }

        [Fact]
        public async Task CreateAccount_Throws_WhenUserIsInactive()
        {
            var user = new ApplicationUser { UserName = "cu_inactive", Email = "cu_inactive@example.com", PhoneNumber = "0000000005", FullName = "CU Inactive", NationalId = "NID3", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = false };
            await _userManager.CreateAsync(user, "Password123!");
            if (!await _roleManager.RoleExistsAsync("Client")) { await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" }); }
            await _userManager.AddToRoleAsync(user, "Client");
            var req = new CheckingAccountReqDto { UserId = user.Id, CurrencyId = _context.Currencies.First().Id, InitialBalance = 10m };
            await Assert.ThrowsAsync<BankingSystemAPI.Application.Exceptions.BadRequestException>(() => _service.CreateAccountAsync(req));
        }

        [Fact]
        public async Task CreateAccount_Throws_WhenCurrencyIsInactive()
        {
            var user = new ApplicationUser { UserName = "cu_active", Email = "cu_active@example.com", PhoneNumber = "0000000006", FullName = "CU Active", NationalId = "NID4", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user, "Password123!");
            if (!await _roleManager.RoleExistsAsync("Client")) { await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" }); }
            await _userManager.AddToRoleAsync(user, "Client");
            var currency = _context.Currencies.First();
            currency.IsActive = false;
            _context.SaveChanges();
            var req = new CheckingAccountReqDto { UserId = user.Id, CurrencyId = currency.Id, InitialBalance = 10m };
            await Assert.ThrowsAsync<BankingSystemAPI.Application.Exceptions.BadRequestException>(() => _service.CreateAccountAsync(req));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

