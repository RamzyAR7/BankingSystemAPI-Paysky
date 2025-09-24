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

namespace BankingSystemAPI.UnitTests
{
    public class UserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _currentUserMock;

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
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new NullLogger<UserManager<ApplicationUser>>() );

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<UserResDto>(It.IsAny<ApplicationUser>()))
                .Returns((ApplicationUser u) => new UserResDto { Id = u.Id, Username = u.UserName, Email = u.Email });

            _mapper = mapperMock.Object;

            var httpAccessorMock = new Mock<IHttpContextAccessor>();
            _currentUserMock = new Mock<ICurrentUserService>();
            // default to SuperAdmin behavior for some tests; individual tests can override
            _currentUserMock.Setup(c => c.IsInRoleAsync(It.IsAny<string>())).ReturnsAsync(true);

            // create a concrete RoleManager to avoid Moq/Castle constructor proxy issues
            var roleStore = new RoleStore<IdentityRole>(_context);
            var roleManager = new RoleManager<IdentityRole>(roleStore,
                new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new NullLogger<RoleManager<IdentityRole>>());

            _service = new UserService(_userManager, _mapper, _currentUserMock.Object, roleManager);

            // seed user (set required fields)
            var user = new ApplicationUser { UserName = "u1", Email = "u1@example.com", PhoneNumber = "0000000000", FullName = "User One", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-30) };
            _userManager.CreateAsync(user, "Password123!").GetAwaiter().GetResult();

            // set current user to seeded user and role as Admin (non-super, non-client) so CreateUserAsync uses acting user's bank scope
            _currentUserMock.SetupGet(c => c.UserId).Returns(user.Id);
            _currentUserMock.Setup(c => c.GetRoleFromStoreAsync()).ReturnsAsync("Admin");
        }

        [Fact]
        public async Task GetUserByUsername_ReturnsDto()
        {
            var dto = await _service.GetUserByUsernameAsync("u1");
            Assert.NotNull(dto);
            Assert.Equal("u1", dto.Username);
            Assert.Equal("u1@example.com", dto.Email);
        }

        [Fact]
        public async Task GetUserByUsername_NotFound_ReturnsNull()
        {
            var dto = await _service.GetUserByUsernameAsync("notexists");
            Assert.Null(dto);
        }

        [Fact]
        public async Task GetUserById_ReturnsDto()
        {
            var existing = _userManager.Users.First();
            var dto = await _service.GetUserByIdAsync(existing.Id);
            Assert.NotNull(dto);
            Assert.Equal(existing.Id, dto.Id);
        }

        [Fact]
        public async Task CreateUser_DuplicateUsername_ReturnsError()
        {
            var req = new UserReqDto
            {
                Username = "u1",
                Email = "u1_new@example.com",
                Password = "Password123!",
                FullName = "Test"
            };

            var res = await _service.CreateUserAsync(req);
            Assert.False(res.Succeeded);
            Assert.NotEmpty(res.Errors);
            // duplicate may be detected by scoped check; accept either message
            Assert.Contains(res.Errors, e => e.Description.Contains("Username") || e.Description.Contains("already exists"));
        }

        [Fact]
        public async Task GetAllUsers_AsSuperAdmin_ReturnsAll()
        {
            // create additional users
            var u2 = new ApplicationUser { UserName = "u2", Email = "u2@example.com", PhoneNumber = "0000000001", FullName = "User Two", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-25) };
            var u3 = new ApplicationUser { UserName = "u3", Email = "u3@example.com", PhoneNumber = "0000000002", FullName = "User Three", NationalId = Guid.NewGuid().ToString().Substring(0,10), DateOfBirth = DateTime.UtcNow.AddYears(-20) };
            // add directly to context to avoid UserManager.CreateAsync side-effects
            _context.Users.AddRange(u2, u3);
            _context.SaveChanges();

            var list = await _service.GetAllUsersAsync(1, 10);
            Assert.NotNull(list);
            Assert.True(list.Count >= 3);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
