using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Repositories;
using BankingSystemAPI.Infrastructure.UnitOfWork;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Mapping;
using AutoMapper;
using BankingSystemAPI.Infrastructure.Services;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;

namespace BankingSystemAPI.UnitTests
{
    public class RoleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _service;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _uow;

        public RoleServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var roleStore = new RoleStore<ApplicationRole>(_context);
            _roleManager = new RoleManager<ApplicationRole>(roleStore,
                new IRoleValidator<ApplicationRole>[] { new RoleValidator<ApplicationRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new NullLogger<RoleManager<ApplicationRole>>());

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
            mapperMock.Setup(m => m.Map<List<RoleResDto>>(It.IsAny<IEnumerable<ApplicationRole>>() ))
                .Returns((IEnumerable<ApplicationRole> roles) => roles.Select(r => new RoleResDto { Name = r.Name }).ToList());

            _mapper = mapperMock.Object;

            _service = new RoleService(_roleManager, _mapper, _context);
        }

        [Fact]
        public async Task CreateAndGetRoles_Works()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" });

            var all = await _service.GetAllRolesAsync();
            Assert.True(all.Any(r => r.Name == "Admin"));
            Assert.True(all.Any(r => r.Name == "Client"));
        }

        [Fact]
        public async Task CreateRole_Duplicate_ReturnsError()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Existing" });
            var res = await _service.CreateRoleAsync(new RoleReqDto { Name = "Existing" });
            Assert.False(res.Succeeded);
            Assert.NotEmpty(res.Errors);
        }

        [Fact]
        public async Task DeleteRole_NonExisting_ReturnsError()
        {
            var res = await _service.DeleteRoleAsync("non-existent-id");
            Assert.False(res.Succeeded);
            Assert.Contains(res.Errors, e => e.Description == "Role not found.");
        }

        [Fact]
        public async Task DeleteRole_Succeeds_WhenNoChildren()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "ToDelete" });
            var role = await _roleManager.FindByNameAsync("ToDelete");

            var res = await _service.DeleteRoleAsync(role.Id);
            Assert.True(res.Succeeded);

            var exists = await _roleManager.FindByIdAsync(role.Id);
            Assert.Null(exists);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}