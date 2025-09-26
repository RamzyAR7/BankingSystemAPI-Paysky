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
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtSettings _jwt;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserAuthorizationService? _userAuth;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JwtSettings> jwt,
            IHttpContextAccessor httpContextAccessor,
            IUserAuthorizationService? userAuth = null)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = jwt.Value;
            _httpContextAccessor = httpContextAccessor;
            _userAuth = userAuth;
        }

        public async Task<AuthResultDto> LoginAsync(LoginReqDto request)
        {
            var result = new AuthResultDto();

            // Load user including Bank so we can check bank active status during login
            var user = await _userManager.Users
                .Include(u => u.Bank)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                result.Errors.Add(new IdentityError { Description = "Email or Password is incorrect!" });
                result.Succeeded = false;
                return result;
            }

            // Block login if user is not active
            if (!user.IsActive)
            {
                result.Errors.Add(new IdentityError { Description = "User account is inactive. Contact administrator." });
                result.Succeeded = false;
                return result;
            }

            // Block login if user's bank is inactive
            if (user.Bank != null && !user.Bank.IsActive)
            {
                result.Errors.Add(new IdentityError { Description = "Cannot login: user's bank is inactive." });
                result.Succeeded = false;
                return result;
            }

            // Ensure user has at least one role before issuing tokens
            var rolesList = await _userManager.GetRolesAsync(user);
            if (rolesList == null || !rolesList.Any())
            {
                result.Errors.Add(new IdentityError { Description = "User has no role assigned. Contact administrator." });
                result.Succeeded = false;
                return result;
            }

            var jwtSecurityToken = await CreateJwtToken(user);

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
            // Load user including refresh tokens to revoke them
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
                token.RevokedOn = DateTime.UtcNow;

            // Rotate security stamp to invalidate existing JWTs immediately
            await _userManager.UpdateSecurityStampAsync(user);

            await _userManager.UpdateAsync(user);
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken");
            result.Succeeded = true;
            result.Message = "Logout successful.";
            return result;
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string? tokenFromRequest = null)
        {
            var result = new AuthResultDto();
            var request = _httpContextAccessor.HttpContext?.Request;
            var token = tokenFromRequest ?? request?.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(token))
            {
                result.Errors.Add(new IdentityError { Description = "No refresh token provided" });
                result.Succeeded = false;
                return result;
            }

            // Ensure RefreshTokens collection is included when querying
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

            // Prevent refresh token use if bank inactive
            if (user.Bank != null && !user.Bank.IsActive)
            {
                result.Errors.Add(new IdentityError { Description = "Cannot refresh token: user's bank is inactive." });
                result.Succeeded = false;
                return result;
            }

            // Use FirstOrDefault to avoid Single throwing if no element is present
            var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == token);
            if (refreshToken == null)
            {
                result.Errors.Add(new IdentityError { Description = "Invalid refresh token" });
                result.Succeeded = false;
                return result;
            }

            // Explicitly check revoked state and return a clear error
            if (refreshToken.RevokedOn != null)
            {
                result.Errors.Add(new IdentityError { Description = "Refresh token has been revoked" });
                result.Succeeded = false;
                return result;
            }

            if (!refreshToken.IsActive)
            {
                result.Errors.Add(new IdentityError { Description = "Refresh token is inactive" });
                result.Succeeded = false;
                return result;
            }
            if (refreshToken.IsAbsoluteExpired)
            {
                result.Errors.Add(new IdentityError { Description = "Refresh token absolute expired, login again" });
                result.Succeeded = false;
                return result;
            }
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

            // Authorization: ensure the caller may access the target user's bank
            await _userAuth?.CanViewUserAsync(userId);

            var activeTokens = user.RefreshTokens.Where(x => x.IsActive).ToList();
            if (!activeTokens.Any())
            {
                result.Errors.Add(new IdentityError { Description = "No active tokens found for user." });
                result.Succeeded = false;
                return result;
            }

            foreach (var refreshToken in activeTokens)
            {
                refreshToken.RevokedOn = DateTime.UtcNow;
            }

            await _userManager.UpdateAsync(user);
            result.Succeeded = true;
            result.Message = "All active tokens revoked successfully for user.";
            return result;
        }

        /// Helper Methods

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            // User has only one role in this system; treat first as primary
            var primaryRole = roles.FirstOrDefault();

            var claimsList = new List<Claim>();

            // Add standard claims
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Sub, user.UserName));
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claimsList.Add(new Claim("uid", user.Id));

            // include security stamp to allow immediate invalidation of JWTs on logout
            var securityStamp = await _userManager.GetSecurityStampAsync(user);
            claimsList.Add(new Claim("sst", securityStamp ?? string.Empty));

            // Add primary role as both ClaimTypes.Role and a "role" claim for convenience
            if (!string.IsNullOrEmpty(primaryRole))
            {
                claimsList.Add(new Claim(ClaimTypes.Role, primaryRole));
                claimsList.Add(new Claim("role", primaryRole));
            }

            // Gather permission and role-specific claims from the primary role (if any)
            var permissionValues = new HashSet<string>();
            if (!string.IsNullOrEmpty(primaryRole))
            {
                var identityRole = await _roleManager.FindByNameAsync(primaryRole);
                if (identityRole != null)
                {
                    var roleSpecificClaims = await _roleManager.GetClaimsAsync(identityRole);
                    foreach (var rc in roleSpecificClaims)
                    {
                        // Add each role-specific claim to token
                        claimsList.Add(new Claim(rc.Type, rc.Value));
                        if (string.Equals(rc.Type, "Permission", StringComparison.OrdinalIgnoreCase))
                            permissionValues.Add(rc.Value);
                    }
                }
            }

            // Add a combined "claims" claim that lists permission values for quick access (optional)
            //if (permissionValues.Any())
            //{
            //    claimsList.Add(new Claim("claims", string.Join(",", permissionValues)));
            //}

            // Add bank id (safe handling if null)
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

            var token = WebEncoders.Base64UrlEncode(randomNumber);

            return new RefreshToken
            {
                Token = token,
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