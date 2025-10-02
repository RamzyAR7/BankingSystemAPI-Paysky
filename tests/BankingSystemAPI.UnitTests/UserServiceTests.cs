using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Infrastructure.Context;
using Moq;
using AutoMapper;
using BankingSystemAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BankingSystemAPI.Application.DTOs.User;
using Microsoft.AspNetCore.Http;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.UnitTests
{
    public class UserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // Use explicit generic UserStore that matches ApplicationRole in the model
            var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(_context);
            _userManager = new UserManager<ApplicationUser>(userStore,
                null, new PasswordHasher<ApplicationUser>(),
                new IUserValidator<ApplicationUser>[0], new IPasswordValidator<ApplicationUser>[0],
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>());

            // Create AutoMapper using ServiceCollection with required dependencies
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging(); // Add logging services required by AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            // create a concrete RoleManager to avoid Moq/Castle constructor proxy issues
            var roleStore = new RoleStore<ApplicationRole>(_context);
            var roleManager = new RoleManager<ApplicationRole>(roleStore,
                new IRoleValidator<ApplicationRole>[] { new RoleValidator<ApplicationRole>() },
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new NullLogger<RoleManager<ApplicationRole>>());

            // Create UserService with proper dependencies
            _service = new UserService(_userManager, roleManager, _mapper);

            // Create a default role first
            var defaultRole = new ApplicationRole { Name = "TestRole" };
            var roleResult = roleManager.CreateAsync(defaultRole).GetAwaiter().GetResult();
            
            // Seed user with all required fields and ensure it's properly saved
            var user = new ApplicationUser 
            { 
                UserName = "u1", 
                Email = "u1@example.com", 
                PhoneNumber = "0000000000", 
                FullName = "User One", 
                NationalId = Guid.NewGuid().ToString().Substring(0, 10), 
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                IsActive = true,
                RoleId = roleResult.Succeeded ? defaultRole.Id : string.Empty,
                EmailConfirmed = true, // Ensure email is confirmed
                PhoneNumberConfirmed = true // Ensure phone is confirmed
            };
            
            // Create user and wait for completion
            var createResult = _userManager.CreateAsync(user, "Password123!").GetAwaiter().GetResult();
            
            // Verify user was created successfully
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            // Ensure context changes are saved
            _context.SaveChanges();
            
            // Verify the user exists
            var verifyUser = _userManager.FindByNameAsync("u1").GetAwaiter().GetResult();
            if (verifyUser == null)
            {
                throw new InvalidOperationException("Test user was not found after creation");
            }
        }

        [Fact]
        public async Task GetUserByUsername_ReturnsDto()
        {
            var result = await _service.GetUserByUsernameAsync("u1");
            Assert.NotNull(result);
            Assert.True(result.Succeeded, $"Service call failed: {string.Join(", ", result.Errors)}");
            Assert.NotNull(result.Value);
            Assert.Equal("u1", result.Value.Username);
            Assert.Equal("u1@example.com", result.Value.Email);
        }

        [Fact]
        public async Task GetUserByUsername_NotFound_ReturnsFailure()
        {
            var result = await _service.GetUserByUsernameAsync("notexists");
            Assert.NotNull(result);
            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task GetUserById_ReturnsDto()
        {
            var existing = _userManager.Users.First();
            var result = await _service.GetUserByIdAsync(existing.Id);
            Assert.NotNull(result);
            Assert.True(result.Succeeded, $"Service call failed: {string.Join(", ", result.Errors)}");
            Assert.NotNull(result.Value);
            Assert.Equal(existing.Id, result.Value.Id);
        }

        [Fact]
        public async Task CreateUser_DuplicateUsername_ReturnsError()
        {
            var req = new UserReqDto
            {
                Username = "u1",
                Email = "u1_new@example.com",
                Password = "Password123!",
                PasswordConfirm = "Password123!",
                FullName = "Test User",
                NationalId = "1234567890123",
                PhoneNumber = "01234567890",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            };

            var res = await _service.CreateUserAsync(req);
            Assert.False(res.Succeeded);
            Assert.NotEmpty(res.Errors);
        }

        [Fact]
        public async Task GetUsersCount_ShouldHaveMultipleUsers()
        {
            // Get the default role ID for consistency
            var defaultRole = await _userManager.GetRolesAsync(_userManager.Users.First()).ContinueWith(t => 
                _context.Roles.FirstOrDefault()?.Id ?? string.Empty);

            // Create additional users using UserManager to ensure they're properly created
            var u2 = new ApplicationUser 
            { 
                UserName = "u2", 
                Email = "u2@example.com", 
                PhoneNumber = "0000000001", 
                FullName = "User Two", 
                NationalId = Guid.NewGuid().ToString().Substring(0, 10), 
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                IsActive = true,
                RoleId = _context.Roles.FirstOrDefault()?.Id ?? string.Empty,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            var u3 = new ApplicationUser 
            { 
                UserName = "u3", 
                Email = "u3@example.com", 
                PhoneNumber = "0000000002", 
                FullName = "User Three", 
                NationalId = Guid.NewGuid().ToString().Substring(0, 10), 
                DateOfBirth = DateTime.UtcNow.AddYears(-20),
                IsActive = true,
                RoleId = _context.Roles.FirstOrDefault()?.Id ?? string.Empty,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            
            // Use UserManager to create users properly
            await _userManager.CreateAsync(u2, "Password123!");
            await _userManager.CreateAsync(u3, "Password123!");
            
            // Ensure context changes are saved
            _context.SaveChanges();

            // Verify users exist by testing individual retrieval methods
            var user1 = await _service.GetUserByUsernameAsync("u1");
            var user2 = await _service.GetUserByUsernameAsync("u2");
            var user3 = await _service.GetUserByUsernameAsync("u3");

            Assert.True(user1.Succeeded, $"User1 failed: {string.Join(", ", user1.Errors)}");
            Assert.True(user2.Succeeded, $"User2 failed: {string.Join(", ", user2.Errors)}");
            Assert.True(user3.Succeeded, $"User3 failed: {string.Join(", ", user3.Errors)}");
            Assert.NotNull(user1.Value);
            Assert.NotNull(user2.Value);
            Assert.NotNull(user3.Value);
        }

        [Fact]
        public async Task CreateUser_ValidUser_ReturnsSuccess()
        {
            var req = new UserReqDto
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "Password123!",
                PasswordConfirm = "Password123!",
                FullName = "New User",
                NationalId = "9876543210987",
                PhoneNumber = "01987654321",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
            };

            var res = await _service.CreateUserAsync(req);
            Assert.True(res.Succeeded);
            Assert.NotNull(res.Value);
            Assert.Equal("newuser", res.Value.Username);
            Assert.Equal("newuser@example.com", res.Value.Email);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}