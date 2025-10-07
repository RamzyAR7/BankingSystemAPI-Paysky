using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using BankingSystemAPI.Infrastructure.Identity;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Setting;
using BankingSystemAPI.Application.DTOs.Auth;
using AutoMapper;
using BankingSystemAPI.UnitTests.TestInfrastructure;

namespace BankingSystemAPI.UnitTests.UnitTests.Infrastructure
{
    public class AuthServiceTests : TestBase
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly AuthService _authService;

        protected override void ConfigureMapperMock(Mock<IMapper> mapperMock) { }

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<ApplicationRole>>();
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, new List<IRoleValidator<ApplicationRole>>(), null, null, null);

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var jwt = Options.Create(new JwtSettings { Key = "testkey0123456789testkey0123456789", Issuer = "test", Audience = "test", AccessTokenExpirationMinutes = 60, RefreshSlidingDays = 7, RefreshAbsoluteDays = 30 });

            _authService = new AuthService(_userManagerMock.Object, _roleManagerMock.Object, jwt, _httpContextAccessorMock.Object, new NullLogger<AuthService>());
        }

        [Fact]
        public async Task LoginAsync_InvalidUser_ReturnsFailure()
        {
            // Arrange: no users in the EF test context
            _userManagerMock.Setup(u => u.Users).Returns(Context.Users);

            var dto = new LoginReqDto { Email = "noone@example.com", Password = "pw" };

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task LoginAsync_ValidUser_ReturnsSuccess()
        {
            var user = CreateTestUser("a@b.com", "a@b.com");

            _userManagerMock.Setup(u => u.Users).Returns(Context.Users);
            _userManagerMock.Setup(u => u.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());

            var dto = new LoginReqDto { Email = "a@b.com", Password = "pw" };
            var result = await _authService.LoginAsync(dto);
            Assert.True(result.Succeeded);
            Assert.NotNull(result.AuthData);
            Assert.True(result.AuthData.IsAuthenticated);
        }
    }
}
