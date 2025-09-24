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
using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Services;

namespace BankingSystemAPI.UnitTests
{
    public class SavingsAccountServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly SavingsAccountService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public SavingsAccountServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // seed currency
            _context.Currencies.Add(new Currency { Code = "USD", ExchangeRate = 1m, IsBase = true });
            _context.SaveChanges();

            var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>(_context);
            _userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            _currentUserMock = new Mock<ICurrentUserService>();

            var currencyRepo = new CurrencyRepository(_context);
            var accountRepo = new AccountRepository(_context);
            var transactionRepo = new TransactionRepository(_context);
            var accountTxRepo = new AccountTransactionRepository(_context);
            var interestLogRepo = new InterestLogRepository(_context);
            var bankRepo = new BankRepository(_context);
            _uow = new UnitOfWork(accountRepo, transactionRepo, accountTxRepo, interestLogRepo, currencyRepo, bankRepo, _context);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<SavingsAccountDto>(It.IsAny<SavingsAccount>()))
                .Returns((SavingsAccount a) => new SavingsAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty, InterestRate = a.InterestRate });
            mapperMock.Setup(m => m.Map<IEnumerable<SavingsAccountDto>>(It.IsAny<IEnumerable<SavingsAccount>>()))
                .Returns((IEnumerable<SavingsAccount> list) => list.Select(a => new SavingsAccountDto { Id = a.Id, AccountNumber = a.AccountNumber, Balance = a.Balance, UserId = a.UserId, CurrencyCode = a.Currency?.Code ?? string.Empty, InterestRate = a.InterestRate }));
            mapperMock.Setup(m => m.Map<SavingsAccount>(It.IsAny<SavingsAccountReqDto>()))
                .Returns((SavingsAccountReqDto req) => new SavingsAccount { UserId = req.UserId, CurrencyId = req.CurrencyId, Balance = req.InitialBalance, InterestRate = req.InterestRate, InterestType = req.InterestType, RowVersion = new byte[8] });

            _mapper = mapperMock.Object;

            _service = new SavingsAccountService(_uow, _mapper, _userManager, _currentUserMock.Object);
        }

        [Fact]
        public async Task CreateSavingsAccount_Succeeds_WhenUserHasRole()
        {
            var user = new ApplicationUser { UserName = "su1", Email = "su1@example.com", PhoneNumber = "4000000001", FullName = "SU1", NationalId = "NID1", DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            await _userManager.CreateAsync(user, "Password123!");
            // ensure role exists
            if (!_context.Roles.Any(r => r.Name == "Client")) { _context.Roles.Add(new ApplicationRole { Name = "Client", NormalizedName = "CLIENT" }); _context.SaveChanges(); }
            await _userManager.AddToRoleAsync(user, "Client");

            var req = new SavingsAccountReqDto { UserId = user.Id, CurrencyId = _context.Currencies.First().Id, InitialBalance = 100m, InterestRate = 1.5m, InterestType = InterestType.Monthly };
            var res = await _service.CreateAccountAsync(req);

            Assert.NotNull(res);
            Assert.Equal(user.Id, res.UserId);
            Assert.Equal(100m, res.Balance);
        }

        [Fact]
        public async Task Withdraw_FromSavings_InsufficientFunds_ThrowsViaTransactionService()
        {
            var user = new ApplicationUser { UserName = "su2", Email = "su2@example.com", PhoneNumber = "4000000002", FullName = "SU2", NationalId = "NID2", DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            await _userManager.CreateAsync(user, "Password123!");

            var sv = new SavingsAccount { AccountNumber = "SV1", Balance = 10m, UserId = user.Id, CurrencyId = _context.Currencies.First().Id, RowVersion = new byte[8] };
            _context.SavingsAccounts.Add(sv);
            _context.SaveChanges();

            // use transaction service to perform withdraw
            var mapperMock = new Mock<IMapper>();
            var txMapper = mapperMock.Object;
            var helperMock = new Mock<ITransactionHelperService>();
            var userStore = new UserStore<ApplicationUser>(_context);
            var userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());
            var currentUserMock = new Mock<ICurrentUserService>();
            var txService = new TransactionService(_uow, txMapper, helperMock.Object, userManager, currentUserMock.Object, new NullLogger<BankingSystemAPI.Application.Services.TransactionService>());

            var req = new WithdrawReqDto { AccountId = sv.Id, Amount = 20m };
            await Assert.ThrowsAsync<InvalidOperationException>(() => txService.WithdrawAsync(req));
        }

        [Fact]
        public async Task CreateSavingsAccount_Throws_WhenUserIsInactive()
        {
            var user = new ApplicationUser { UserName = "su_inactive", Email = "su_inactive@example.com", PhoneNumber = "0000000001", FullName = "SU Inactive", NationalId = "NID3", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = false };
            await _userManager.CreateAsync(user, "Password123!");
            if (!_context.Roles.Any(r => r.Name == "Client")) { _context.Roles.Add(new ApplicationRole { Name = "Client", NormalizedName = "CLIENT" }); _context.SaveChanges(); }
            await _userManager.AddToRoleAsync(user, "Client");
            var req = new SavingsAccountReqDto { UserId = user.Id, CurrencyId = _context.Currencies.First().Id, InitialBalance = 100m, InterestRate = 1.5m, InterestType = InterestType.Monthly };
            await Assert.ThrowsAsync<BankingSystemAPI.Application.Exceptions.BadRequestException>(() => _service.CreateAccountAsync(req));
        }

        [Fact]
        public async Task CreateSavingsAccount_Throws_WhenCurrencyIsInactive()
        {
            var user = new ApplicationUser { UserName = "su_active", Email = "su_active@example.com", PhoneNumber = "0000000002", FullName = "SU Active", NationalId = "NID4", DateOfBirth = DateTime.UtcNow.AddYears(-30), IsActive = true };
            await _userManager.CreateAsync(user, "Password123!");
            if (!_context.Roles.Any(r => r.Name == "Client")) { _context.Roles.Add(new ApplicationRole { Name = "Client", NormalizedName = "CLIENT" }); _context.SaveChanges(); }
            await _userManager.AddToRoleAsync(user, "Client");
            var currency = _context.Currencies.First();
            currency.IsActive = false;
            _context.SaveChanges();
            var req = new SavingsAccountReqDto { UserId = user.Id, CurrencyId = currency.Id, InitialBalance = 100m, InterestRate = 1.5m, InterestType = InterestType.Monthly };
            await Assert.ThrowsAsync<BankingSystemAPI.Application.Exceptions.BadRequestException>(() => _service.CreateAccountAsync(req));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}