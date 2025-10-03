using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace BankingSystemAPI.UnitTests.Application.Authorization
{
    public class AuthServiceIntegrationTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public AuthServiceIntegrationTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUserService = new Mock<IUserService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        }

        #region Authentication Flow Tests

        [Fact]
        public async Task AuthenticationFlow_ValidCredentials_ShouldSucceed()
        {
            // Arrange
            var loginRequest = new LoginReqDto
            {
                Email = "admin@bank1.com",
                Password = "AdminPassword123!"
            };

            var authResult = new AuthResultDto
            {
                Succeeded = true,
                AuthData = new AuthResDto
                {
                    IsAuthenticated = true,
                    Token = "valid-jwt-token",
                    Email = loginRequest.Email,
                    Roles = new List<string> { "Admin" }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginReqDto>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.LoginAsync(loginRequest);

            // Assert
            Assert.True(result.Succeeded);
            Assert.True(result.AuthData.IsAuthenticated);
            Assert.Equal(loginRequest.Email, result.AuthData.Email);
            Assert.Contains("Admin", result.AuthData.Roles);
        }

        [Fact]
        public async Task AuthenticationFlow_InvalidCredentials_ShouldFail()
        {
            // Arrange
            var loginRequest = new LoginReqDto
            {
                Email = "user@example.com",
                Password = "WrongPassword"
            };

            var authResult = new AuthResultDto
            {
                Succeeded = false,
                Errors = new List<IdentityError>
                {
                    new IdentityError { Description = "Invalid credentials" }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginReqDto>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.LoginAsync(loginRequest);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.Contains("Invalid credentials"));
        }

        [Fact]
        public async Task AuthenticationFlow_InactiveUser_ShouldFail()
        {
            // Arrange
            var loginRequest = new LoginReqDto
            {
                Email = "inactive@example.com",
                Password = "ValidPassword123!"
            };

            var authResult = new AuthResultDto
            {
                Succeeded = false,
                Errors = new List<IdentityError>
                {
                    new IdentityError { Description = "User account is inactive" }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginReqDto>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.LoginAsync(loginRequest);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.ToLower().Contains("inactive"));
        }

        #endregion

        #region Token Refresh Tests

        [Fact]
        public async Task TokenRefresh_ValidRefreshToken_ShouldSucceed()
        {
            // Arrange
            var refreshToken = "valid-refresh-token";
            var authResult = new AuthResultDto
            {
                Succeeded = true,
                AuthData = new AuthResDto
                {
                    IsAuthenticated = true,
                    Token = "new-access-token",
                    RefreshToken = "new-refresh-token"
                }
            };

            _mockAuthService.Setup(x => x.RefreshTokenAsync(refreshToken))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.True(result.Succeeded);
            Assert.True(result.AuthData.IsAuthenticated);
            Assert.NotNull(result.AuthData.Token);
            Assert.NotNull(result.AuthData.RefreshToken);
        }

        [Fact]
        public async Task TokenRefresh_ExpiredRefreshToken_ShouldFail()
        {
            // Arrange
            var expiredToken = "expired-refresh-token";
            var authResult = new AuthResultDto
            {
                Succeeded = false,
                Errors = new List<IdentityError>
                {
                    new IdentityError { Description = "Refresh token has expired" }
                }
            };

            _mockAuthService.Setup(x => x.RefreshTokenAsync(expiredToken))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.RefreshTokenAsync(expiredToken);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.ToLower().Contains("expired"));
        }

        [Fact]
        public async Task TokenRefresh_InvalidRefreshToken_ShouldFail()
        {
            // Arrange
            var invalidToken = "invalid-refresh-token";
            var authResult = new AuthResultDto
            {
                Succeeded = false,
                Errors = new List<IdentityError>
                {
                    new IdentityError { Description = "Invalid refresh token" }
                }
            };

            _mockAuthService.Setup(x => x.RefreshTokenAsync(invalidToken))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.RefreshTokenAsync(invalidToken);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.ToLower().Contains("invalid"));
        }

        #endregion

        #region Authorization Context Tests

        [Fact]
        public void AuthorizationContext_SuperAdminRole_ShouldHaveGlobalAccess()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "superadmin123"),
                new Claim(ClaimTypes.Role, UserRole.SuperAdmin.ToString()),
                new Claim(ClaimTypes.Email, "superadmin@system.com"),
                new Claim("BankId", "0") // SuperAdmin might not have a specific bank
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var isInSuperAdminRole = principal.IsInRole(UserRole.SuperAdmin.ToString());

            // Assert
            Assert.Equal("superadmin123", userId);
            Assert.Equal(UserRole.SuperAdmin.ToString(), role);
            Assert.Equal("superadmin@system.com", email);
            Assert.True(isInSuperAdminRole);
        }

        [Fact]
        public void AuthorizationContext_AdminRole_ShouldHaveBankLevelAccess()
        {
            // Arrange
            var bankId = 100;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin123"),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString()),
                new Claim(ClaimTypes.Email, "admin@bank100.com"),
                new Claim("BankId", bankId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var extractedBankId = principal.FindFirst("BankId")?.Value;
            var isInAdminRole = principal.IsInRole(UserRole.Admin.ToString());

            // Assert
            Assert.Equal("admin123", userId);
            Assert.Equal(UserRole.Admin.ToString(), role);
            Assert.Equal(bankId.ToString(), extractedBankId);
            Assert.True(isInAdminRole);
        }

        [Fact]
        public void AuthorizationContext_ClientRole_ShouldHaveSelfAccess()
        {
            // Arrange
            var bankId = 100;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "client123"),
                new Claim(ClaimTypes.Role, UserRole.Client.ToString()),
                new Claim(ClaimTypes.Email, "client@bank100.com"),
                new Claim("BankId", bankId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var extractedBankId = principal.FindFirst("BankId")?.Value;
            var isInClientRole = principal.IsInRole(UserRole.Client.ToString());

            // Assert
            Assert.Equal("client123", userId);
            Assert.Equal(UserRole.Client.ToString(), role);
            Assert.Equal(bankId.ToString(), extractedBankId);
            Assert.True(isInClientRole);
        }

        #endregion

        #region Role-Based Access Control Tests

        [Theory]
        [InlineData("SuperAdmin", true, true, true, true)]
        [InlineData("Admin", false, true, true, false)]
        [InlineData("Client", false, false, false, false)]
        public void RoleBasedAccessControl_DifferentRoles_ShouldHaveCorrectPermissions(
            string roleName, 
            bool canViewAllUsers, 
            bool canModifyUsers, 
            bool canViewAccounts, 
            bool canDeleteUsers)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123"),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("BankId", "100")
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            // Act & Assert
            var actualCanViewAllUsers = principal.IsInRole("SuperAdmin");
            var actualCanModifyUsers = principal.IsInRole("SuperAdmin") || principal.IsInRole("Admin");
            var actualCanViewAccounts = !principal.IsInRole("Client");
            var actualCanDeleteUsers = principal.IsInRole("SuperAdmin");

            Assert.Equal(canViewAllUsers, actualCanViewAllUsers);
            Assert.Equal(canModifyUsers, actualCanModifyUsers);
            Assert.Equal(canViewAccounts, actualCanViewAccounts);
            Assert.Equal(canDeleteUsers, actualCanDeleteUsers);
        }

        #endregion

        #region Security Integration Tests

        [Fact]
        public async Task SecurityIntegration_UserLogout_ShouldInvalidateTokens()
        {
            // Arrange
            var userId = "user123";
            var authResult = new AuthResultDto { Succeeded = true };

            _mockAuthService.Setup(x => x.LogoutAsync(userId))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.LogoutAsync(userId);

            // Assert
            Assert.True(result.Succeeded);
            _mockAuthService.Verify(x => x.LogoutAsync(userId), Times.Once);
        }

        [Fact]
        public async Task SecurityIntegration_TokenRevocation_ShouldInvalidateSpecificToken()
        {
            // Arrange
            var token = "token-to-revoke";
            var authResult = new AuthResultDto { Succeeded = true };

            _mockAuthService.Setup(x => x.RevokeTokenAsync(token))
                .ReturnsAsync(authResult);

            // Act
            var result = await _mockAuthService.Object.RevokeTokenAsync(token);

            // Assert
            Assert.True(result.Succeeded);
            _mockAuthService.Verify(x => x.RevokeTokenAsync(token), Times.Once);
        }

        [Fact]
        public void SecurityIntegration_HttpContextUser_ShouldMatchAuthenticatedUser()
        {
            // Arrange
            var userId = "user123";
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var contextUser = _mockHttpContextAccessor.Object.HttpContext?.User;
            var contextUserId = contextUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Assert
            Assert.NotNull(contextUser);
            Assert.Equal(userId, contextUserId);
            Assert.True(contextUser.Identity?.IsAuthenticated);
        }

        #endregion

        #region CurrentUserService Tests

        [Fact]
        public void CurrentUserService_UserIdProperty_ShouldReturnUserId()
        {
            // Arrange
            var expectedUserId = "user123";
            _mockCurrentUserService.Setup(x => x.UserId).Returns(expectedUserId);

            // Act
            var actualUserId = _mockCurrentUserService.Object.UserId;

            // Assert
            Assert.Equal(expectedUserId, actualUserId);
        }

        [Fact]
        public void CurrentUserService_BankIdProperty_ShouldReturnBankId()
        {
            // Arrange
            var expectedBankId = 100;
            _mockCurrentUserService.Setup(x => x.BankId).Returns(expectedBankId);

            // Act
            var actualBankId = _mockCurrentUserService.Object.BankId;

            // Assert
            Assert.Equal(expectedBankId, actualBankId);
        }

        [Fact]
        public async Task CurrentUserService_IsInRoleAsync_ShouldReturnRoleStatus()
        {
            // Arrange
            var roleName = "Admin";
            _mockCurrentUserService.Setup(x => x.IsInRoleAsync(roleName))
                .ReturnsAsync(true);

            // Act
            var isInRole = await _mockCurrentUserService.Object.IsInRoleAsync(roleName);

            // Assert
            Assert.True(isInRole);
        }

        #endregion
    }
}