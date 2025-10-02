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
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.UnitTests
{
    public class UserRolesServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRolesService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserRolesServiceTests()
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

            var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>(_context);
            _userManager = new UserManager<ApplicationUser>(userStore, null, new PasswordHasher<ApplicationUser>(), new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            _service = new UserRolesService(_userManager, _roleManager);
        }

        [Fact]
        public async Task AssignRole_Succeeds()
        {
            // create role and user
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            var user = new ApplicationUser { UserName = "u1", Email = "u1@example.com", PhoneNumber = "5000000001", FullName = "User One", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            await _userManager.CreateAsync(user, "Password123!");

            var dto = new UpdateUserRolesDto { UserId = user.Id, Role = "Admin" };
            var result = await _service.UpdateUserRolesAsync(dto);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.Equal("Admin", result.Value.UserRole.Role);

            var roles = await _userManager.GetRolesAsync(user);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public async Task RemoveAllRoles_WhenRoleNull_RemovesRoles()
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Client" });
            var user = new ApplicationUser { UserName = "u3", Email = "u3@example.com", PhoneNumber = "5000000002", FullName = "User Three", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            await _userManager.CreateAsync(user, "Password123!");
            await _userManager.AddToRoleAsync(user, "Client");

            var dto = new UpdateUserRolesDto { UserId = user.Id, Role = null };
            var result = await _service.UpdateUserRolesAsync(dto);

            Assert.True(result.Succeeded);
            var roles = await _userManager.GetRolesAsync(user);
            Assert.Empty(roles);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
