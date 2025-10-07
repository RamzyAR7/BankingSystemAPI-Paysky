#region Usings
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Setting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
#endregion


namespace BankingSystemAPI.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtSettings _jwt;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JwtSettings> jwt,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = jwt.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResultDto> LoginAsync(LoginReqDto request)
        {
            // Use ResultExtensions for validation chain
            var userResult = await ValidateAndFindUserAsync(request);
            if (userResult.IsFailure)
                return CreateFailureAuthResult(userResult.Errors);

            var tokenResult = await GenerateAuthTokensAsync(userResult.Value!);
            if (tokenResult.IsFailure)
                return CreateFailureAuthResult(tokenResult.Errors);

            var loginResult = CreateSuccessAuthResult(tokenResult.Value!);

            // Add side effects using ResultExtensions
            var result = Result<AuthResultDto>.Success(loginResult);
            result.OnSuccess(() =>
                {
                    _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning("Login failed for: {Email}. Errors: {Errors}",
                        request.Email, string.Join(", ", errors));
                });

            return loginResult;
        }

        public async Task<AuthResultDto> LogoutAsync(string userId)
        {
            var userResult = await FindUserForLogoutAsync(userId);
            if (userResult.IsFailure)
                return CreateFailureAuthResult(userResult.Errors);

            var logoutResult = await ExecuteLogoutAsync(userResult.Value!, userId);

            // Add side effects using ResultExtensions
            logoutResult.OnSuccess(() =>
                {
                    _logger.LogInformation("User logged out successfully: {UserId}", userId);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning("Logout failed for: {UserId}. Errors: {Errors}",
                        userId, string.Join(", ", errors));
                });

            return logoutResult.IsSuccess
                ? CreateSuccessLogoutResult()
                : CreateFailureAuthResult(logoutResult.Errors);
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string? tokenFromRequest = null)
        {
            var tokenValidationResult = await ValidateRefreshTokenAsync(tokenFromRequest);
            if (tokenValidationResult.IsFailure)
                return CreateFailureAuthResult(tokenValidationResult.Errors);

            var refreshResult = await ExecuteTokenRefreshAsync(tokenValidationResult.Value!);

            // Add side effects using ResultExtensions
            refreshResult.OnSuccess(() =>
                {
                    _logger.LogDebug("Token refreshed successfully for user: {UserId}", tokenValidationResult.Value!.User.Id);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning("Token refresh failed. Errors: {Errors}",
                        string.Join(", ", errors));
                });

            return refreshResult.IsSuccess
                ? refreshResult.Value!
                : CreateFailureAuthResult(refreshResult.Errors);
        }

        public async Task<AuthResultDto> RevokeTokenAsync(string userId)
        {
            var userResult = await FindUserForTokenRevocationAsync(userId);
            if (userResult.IsFailure)
                return CreateFailureAuthResult(userResult.Errors);

            var revokeResult = await ExecuteTokenRevocationAsync(userResult.Value!, userId);

            // Add side effects using ResultExtensions
            revokeResult.OnSuccess(() =>
                {
                    _logger.LogInformation("Tokens revoked successfully for user: {UserId}", userId);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning("Token revocation failed for: {UserId}. Errors: {Errors}",
                        userId, string.Join(", ", errors));
                });

            return revokeResult.IsSuccess
                ? CreateSuccessRevocationResult()
                : CreateFailureAuthResult(revokeResult.Errors);
        }

        #region Private Helper Methods Using ResultExtensions

        private async Task<Result<ApplicationUser>> ValidateAndFindUserAsync(LoginReqDto request)
        {
            var user = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return Result<ApplicationUser>.BadRequest("Email or Password is incorrect!");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            return passwordValid
                ? Result<ApplicationUser>.Success(user)
                : Result<ApplicationUser>.BadRequest("Email or Password is incorrect!");
        }

        private async Task<Result<TokenData>> GenerateAuthTokensAsync(ApplicationUser user)
        {
            try
            {
                var rolesList = await _userManager.GetRolesAsync(user);
                var jwtSecurityToken = await CreateJwtToken(user);

                // Clean up inactive tokens
                var inactiveTokens = user.RefreshTokens.Where(t => !t.IsActive).ToList();
                foreach (var inactive in inactiveTokens)
                {
                    user.RefreshTokens.Remove(inactive);
                }

                var refreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(refreshToken);
                await _userManager.UpdateAsync(user);
                SetRefreshTokenInCookie(refreshToken);

                var tokenData = new TokenData
                {
                    JwtToken = jwtSecurityToken,
                    RefreshToken = refreshToken,
                    Roles = rolesList.ToList(),
                    User = user
                };

                return Result<TokenData>.Success(tokenData);
            }
            catch (Exception ex)
            {
                return Result<TokenData>.BadRequest($"Failed to generate tokens: {ex.Message}");
            }
        }

        private async Task<Result<ApplicationUser>> FindUserForLogoutAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.Id == userId);

            return user.ToResult($"User with ID '{userId}' not found.");
        }

        private async Task<Result> ExecuteLogoutAsync(ApplicationUser user, string userId)
        {
            try
            {
                // Revoke all active refresh tokens
                foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
                    token.RevokedOn = DateTime.UtcNow;

                await _userManager.UpdateSecurityStampAsync(user);
                await _userManager.UpdateAsync(user);

                _httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken");
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.BadRequest($"Failed to logout user: {ex.Message}");
            }
        }

        private async Task<Result<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(string? tokenFromRequest)
        {
            var token = tokenFromRequest ?? _httpContextAccessor.HttpContext?.Request?.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return Result<RefreshTokenValidationResult>.BadRequest("No refresh token provided");

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Bank)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
                return Result<RefreshTokenValidationResult>.BadRequest("Invalid refresh token");

            // Business validation using ResultExtensions patterns
            var bankValidation = user.Bank?.IsActive != false
                ? Result.Success()
                : Result.BadRequest("Cannot refresh token: user's bank is inactive.");

            if (bankValidation.IsFailure)
                return Result<RefreshTokenValidationResult>.Failure(bankValidation.ErrorItems);

            var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == token);
            var tokenValidation = ValidateRefreshTokenState(refreshToken);

            if (tokenValidation.IsFailure)
                return Result<RefreshTokenValidationResult>.Failure(tokenValidation.ErrorItems);

            return Result<RefreshTokenValidationResult>.Success(new RefreshTokenValidationResult
            {
                User = user,
                RefreshToken = refreshToken!
            });
        }

        private Result ValidateRefreshTokenState(RefreshToken? refreshToken)
        {
            if (refreshToken == null || refreshToken.RevokedOn != null || !refreshToken.IsActive || refreshToken.IsAbsoluteExpired)
                return Result.BadRequest("Invalid or expired refresh token");

            return Result.Success();
        }

        // Continue with existing helper methods...
        private async Task<Result<AuthResultDto>> ExecuteTokenRefreshAsync(RefreshTokenValidationResult validationResult)
        {
            try
            {
                var user = validationResult.User;
                var oldRefreshToken = validationResult.RefreshToken;

                // Clean up and create new tokens
                var inactiveTokens = user.RefreshTokens.Where(t => !t.IsActive).ToList();
                foreach (var inactive in inactiveTokens)
                {
                    user.RefreshTokens.Remove(inactive);
                }

                oldRefreshToken.RevokedOn = DateTime.UtcNow;
                var newRefreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(newRefreshToken);
                await _userManager.UpdateAsync(user);
                SetRefreshTokenInCookie(newRefreshToken);

                var jwtToken = await CreateJwtToken(user);
                var roles = await _userManager.GetRolesAsync(user);

                var authResult = new AuthResultDto
                {
                    AuthData = new AuthResDto
                    {
                        IsAuthenticated = true,
                        Email = user.Email,
                        Username = user.UserName,
                        Roles = roles.ToList(),
                        Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        ExpiresOn = jwtToken.ValidTo,
                        RefreshTokenExpiration = newRefreshToken.ExpiresOn,
                        AbsoluteExpiresOn = newRefreshToken.AbsoluteExpiresOn
                    },
                    Succeeded = true
                };

                return Result<AuthResultDto>.Success(authResult);
            }
            catch (Exception ex)
            {
                return Result<AuthResultDto>.BadRequest($"Failed to refresh token: {ex.Message}");
            }
        }

        private async Task<Result<ApplicationUser>> FindUserForTokenRevocationAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.Id == userId);

            return user.ToResult($"User with ID '{userId}' not found.");
        }

        private async Task<Result> ExecuteTokenRevocationAsync(ApplicationUser user, string userId)
        {
            var activeTokens = user.RefreshTokens.Where(x => x.IsActive).ToList();
            if (!activeTokens.Any())
                return Result.BadRequest("No active tokens found for user.");

            try
            {
                foreach (var refreshToken in activeTokens)
                {
                    refreshToken.RevokedOn = DateTime.UtcNow;
                }

                await _userManager.UpdateAsync(user);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.BadRequest($"Failed to revoke tokens: {ex.Message}");
            }
        }

        // Result creation helpers
        private AuthResultDto CreateFailureAuthResult(IReadOnlyList<string> errors)
        {
            var result = new AuthResultDto { Succeeded = false };
            foreach (var error in errors)
            {
                result.Errors.Add(new IdentityError { Description = error });
            }
            return result;
        }

        private AuthResultDto CreateSuccessAuthResult(TokenData tokenData)
        {
            return new AuthResultDto
            {
                AuthData = new AuthResDto
                {
                    Message = "Login successful",
                    IsAuthenticated = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(tokenData.JwtToken),
                    Email = tokenData.User.Email,
                    Username = tokenData.User.UserName,
                    ExpiresOn = tokenData.JwtToken.ValidTo,
                    Roles = tokenData.Roles,
                    RefreshTokenExpiration = tokenData.RefreshToken.ExpiresOn,
                    AbsoluteExpiresOn = tokenData.RefreshToken.AbsoluteExpiresOn
                },
                Succeeded = true
            };
        }

        private AuthResultDto CreateSuccessLogoutResult()
        {
            return new AuthResultDto { Succeeded = true, Message = "Logout successful." };
        }

        private AuthResultDto CreateSuccessRevocationResult()
        {
            return new AuthResultDto { Succeeded = true, Message = "All active tokens revoked successfully for user." };
        }

        #endregion

        #region Original Helper Methods (unchanged for stability)

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault();
            var claimsList = new List<Claim>();

            // Add standard claims
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Sub, user.UserName));
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claimsList.Add(new Claim("uid", user.Id));

            // Add security stamp for immediate invalidation
            var securityStamp = await _userManager.GetSecurityStampAsync(user);
            claimsList.Add(new Claim("sst", securityStamp ?? string.Empty));

            // Add role claims
            if (!string.IsNullOrEmpty(primaryRole))
            {
                claimsList.Add(new Claim(ClaimTypes.Role, primaryRole));
                claimsList.Add(new Claim("role", primaryRole));

                // Add role-specific claims
                var identityRole = await _roleManager.FindByNameAsync(primaryRole);
                if (identityRole != null)
                {
                    var roleSpecificClaims = await _roleManager.GetClaimsAsync(identityRole);
                    foreach (var rc in roleSpecificClaims)
                    {
                        claimsList.Add(new Claim(rc.Type, rc.Value));
                    }
                }
            }

            // Add bank id
            claimsList.Add(new Claim("bankid", user.BankId?.ToString() ?? string.Empty));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claimsList,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );
        }

        private RefreshToken GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomNumber);

            return new RefreshToken
            {
                Token = WebEncoders.Base64UrlEncode(randomNumber),
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(_jwt.RefreshSlidingDays),
                AbsoluteExpiresOn = DateTime.UtcNow.AddDays(_jwt.RefreshAbsoluteDays)
            };
        }

        private void SetRefreshTokenInCookie(RefreshToken refreshToken)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresOn
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken.Token, options);
        }

        #endregion

        #region Helper Classes

        private class TokenData
        {
            public JwtSecurityToken JwtToken { get; set; } = null!;
            public RefreshToken RefreshToken { get; set; } = null!;
            public List<string> Roles { get; set; } = new();
            public ApplicationUser User { get; set; } = null!;
        }

        private class RefreshTokenValidationResult
        {
            public ApplicationUser User { get; set; } = null!;
            public RefreshToken RefreshToken { get; set; } = null!;
        }

        #endregion
    }
}
