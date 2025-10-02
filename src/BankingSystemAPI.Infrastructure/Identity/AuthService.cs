using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Setting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace BankingSystemAPI.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtSettings _jwt;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JwtSettings> jwt,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = jwt.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResultDto> LoginAsync(LoginReqDto request)
        {
            var result = new AuthResultDto();

            // Find user by email
            var user = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                result.Errors.Add(new IdentityError { Description = "Email or Password is incorrect!" });
                result.Succeeded = false;
                return result;
            }

            // Get user roles
            var rolesList = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var jwtSecurityToken = await CreateJwtToken(user);

            // Clean up inactive tokens and add new refresh token
            var inactiveTokens = user.RefreshTokens.Where(t => !t.IsActive).ToList();
            foreach (var inactive in inactiveTokens)
            {
                user.RefreshTokens.Remove(inactive);
            }

            var refreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(user);
            SetRefreshTokenInCookie(refreshToken);

            result.AuthData = new AuthResDto
            {
                Message = "Login successful",
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Email = user.Email,
                Username = user.UserName,
                ExpiresOn = jwtSecurityToken.ValidTo,
                Roles = rolesList.ToList(),
                RefreshTokenExpiration = refreshToken.ExpiresOn,
                AbsoluteExpiresOn = refreshToken.AbsoluteExpiresOn
            };
            result.Succeeded = true;
            return result;
        }

        public async Task<AuthResultDto> LogoutAsync(string userId)
        {
            var result = new AuthResultDto();

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            // Revoke all active refresh tokens
            foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
                token.RevokedOn = DateTime.UtcNow;

            // Update security stamp to invalidate JWTs
            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.UpdateAsync(user);

            // Delete refresh token cookie
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken");

            result.Succeeded = true;
            result.Message = "Logout successful.";
            return result;
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string? tokenFromRequest = null)
        {
            var result = new AuthResultDto();
            
            // Use the provided token, or fallback to cookies for backward compatibility
            var token = tokenFromRequest ?? _httpContextAccessor.HttpContext?.Request?.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
            {
                result.Errors.Add(new IdentityError { Description = "No refresh token provided" });
                result.Succeeded = false;
                return result;
            }

            // Find user by refresh token
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Bank)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
            {
                result.Errors.Add(new IdentityError { Description = "Invalid refresh token" });
                result.Succeeded = false;
                return result;
            }

            // Business validation: Check if user's bank is active
            if (user.Bank != null && !user.Bank.IsActive)
            {
                result.Errors.Add(new IdentityError { Description = "Cannot refresh token: user's bank is inactive." });
                result.Succeeded = false;
                return result;
            }

            var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == token);
            if (refreshToken == null || refreshToken.RevokedOn != null || !refreshToken.IsActive || refreshToken.IsAbsoluteExpired)
            {
                result.Errors.Add(new IdentityError { Description = "Invalid or expired refresh token" });
                result.Succeeded = false;
                return result;
            }

            // Clean up inactive tokens and create new refresh token
            var inactiveTokens = user.RefreshTokens.Where(t => !t.IsActive).ToList();
            foreach (var inactive in inactiveTokens)
            {
                user.RefreshTokens.Remove(inactive);
            }

            refreshToken.RevokedOn = DateTime.UtcNow;
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);
            SetRefreshTokenInCookie(newRefreshToken);

            var jwtToken = await CreateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            result.AuthData = new AuthResDto
            {
                IsAuthenticated = true,
                Email = user.Email,
                Username = user.UserName,
                Roles = roles.ToList(),
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                RefreshTokenExpiration = newRefreshToken.ExpiresOn,
                AbsoluteExpiresOn = newRefreshToken.AbsoluteExpiresOn
            };
            result.Succeeded = true;
            return result;
        }

        public async Task<AuthResultDto> RevokeTokenAsync(string userId)
        {
            var result = new AuthResultDto();

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            var activeTokens = user.RefreshTokens.Where(x => x.IsActive).ToList();
            if (!activeTokens.Any())
            {
                result.Errors.Add(new IdentityError { Description = "No active tokens found for user." });
                result.Succeeded = false;
                return result;
            }

            // Revoke all active tokens
            foreach (var refreshToken in activeTokens)
            {
                refreshToken.RevokedOn = DateTime.UtcNow;
            }

            await _userManager.UpdateAsync(user);
            result.Succeeded = true;
            result.Message = "All active tokens revoked successfully for user.";
            return result;
        }

        // Helper Methods
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
    }
}