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
        private readonly DefaultHttpContext _httpContext;

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

            _httpContext = new DefaultHttpContext();
            var httpAccessor = new HttpContextAccessor { HttpContext = _httpContext };

            _service = new RoleClaimsService(_roleManager, httpAccessor);
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
            var res = await _service.UpdateRoleClaimsAsync(dto);

            Assert.True(res.Succeeded);
            Assert.Equal(2, res.RoleClaims.Claims.Count);
            var claims = await _roleManager.GetClaimsAsync(role);
            Assert.Contains(claims, c => c.Value == "C1");
            Assert.Contains(claims, c => c.Value == "C2");
        }

        [Fact]
        public async Task UpdateClaims_SuperAdminRole_Fails()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "SuperAdmin" });

            var dto = new UpdateRoleClaimsDto { RoleName = "SuperAdmin", Claims = new List<string> { "C1" } };
            var res = await _service.UpdateRoleClaimsAsync(dto);

            Assert.False(res.Succeeded);
            Assert.Contains(res.Errors, e => e.Description.Contains("Cannot modify claims for SuperAdmin"));
        }

        [Fact]
        public async Task UpdateClaims_ClientRole_OnlySuperAdminAllowed()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" });
            // http context user not in SuperAdmin
            _httpContext.User = new System.Security.Claims.ClaimsPrincipal();

            var dto = new UpdateRoleClaimsDto { RoleName = "Client", Claims = new List<string> { "C1" } };
            var res = await _service.UpdateRoleClaimsAsync(dto);

            Assert.False(res.Succeeded);
            Assert.Contains(res.Errors, e => e.Description.Contains("Only SuperAdmin can modify claims for Client"));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
