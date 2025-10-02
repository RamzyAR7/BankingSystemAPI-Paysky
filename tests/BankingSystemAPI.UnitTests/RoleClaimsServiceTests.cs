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
using BankingSystemAPI.Infrastructure.Services;
using Moq;
using Microsoft.AspNetCore.Http;
using BankingSystemAPI.Application.DTOs.Role;

namespace BankingSystemAPI.UnitTests
{
    public class RoleClaimsServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleClaimsService _service;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleClaimsServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            var roleStore = new RoleStore<ApplicationRole>(_context);
            _roleManager = new RoleManager<ApplicationRole>(roleStore,
                new IRoleValidator<ApplicationRole>[] { new RoleValidator<ApplicationRole>() },
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new NullLogger<RoleManager<ApplicationRole>>());

            // Updated constructor to match simplified RoleClaimsService
            _service = new RoleClaimsService(_roleManager);
        }

        [Fact]
        public async Task UpdateClaims_RemovesAndAddsClaims_Succeeds()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            var role = await _roleManager.FindByNameAsync("Admin");

            // add initial claims
            await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "P1"));
            await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", "P2"));

            var dto = new UpdateRoleClaimsDto { RoleName = "Admin", Claims = new List<string> { "C1", "C2" } };
            var result = await _service.UpdateRoleClaimsAsync(dto);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.RoleClaims.Claims.Count);
            var claims = await _roleManager.GetClaimsAsync(role);
            Assert.Contains(claims, c => c.Value == "C1");
            Assert.Contains(claims, c => c.Value == "C2");
        }

        [Fact]
        public async Task UpdateClaims_RoleNotFound_Fails()
        {
            var dto = new UpdateRoleClaimsDto { RoleName = "NonExistent", Claims = new List<string> { "C1" } };
            var result = await _service.UpdateRoleClaimsAsync(dto);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Contains("Role not found"));
        }

        [Fact]
        public async Task GetAllClaimsByGroup_ReturnsGroupedClaims()
        {
            var result = await _service.GetAllClaimsByGroup();
            
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.NotEmpty(result.Value);
            
            // Should contain groups for different controller types
            Assert.Contains(result.Value, group => group.Name == "User");
            Assert.Contains(result.Value, group => group.Name == "Role");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
