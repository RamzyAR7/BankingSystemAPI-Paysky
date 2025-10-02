using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using BankingSystemAPI.Infrastructure.Services;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Moq;
using BankingSystemAPI.Application.DTOs.Role;

namespace BankingSystemAPI.UnitTests
{
    public class RoleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _service;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;

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

            var mapperMock = new Mock<IMapper>();
            
            // Setup for list mapping
            mapperMock.Setup(m => m.Map<List<RoleResDto>>(It.IsAny<IEnumerable<ApplicationRole>>()))
                .Returns((IEnumerable<ApplicationRole> roles) => roles.Select(r => new RoleResDto { Name = r.Name }).ToList());
            
            // Setup for individual mapping
            mapperMock.Setup(m => m.Map<RoleResDto>(It.IsAny<ApplicationRole>()))
                .Returns((ApplicationRole role) => role == null ? null : new RoleResDto { Name = role.Name });

            _mapper = mapperMock.Object;

            _service = new RoleService(_roleManager, _mapper);
        }

        [Fact]
        public async Task CreateAndGetRoles_Works()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" });

            var result = await _service.GetAllRolesAsync();
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Any(r => r.Name == "Admin"));
            Assert.True(result.Value.Any(r => r.Name == "Client"));
        }

        [Fact]
        public async Task CreateRole_Succeeds()
        {
            var result = await _service.CreateRoleAsync(new RoleReqDto { Name = "NewRole" });
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.Role);
            Assert.Equal("NewRole", result.Value.Role.Name);
        }

        [Fact]
        public async Task DeleteRole_NonExisting_ReturnsError()
        {
            var result = await _service.DeleteRoleAsync("non-existent-id");
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains("Role not found"));
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