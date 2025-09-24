using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Authentication endpoints (login, refresh, logout, revoke tokens).
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login with credentials and receive authentication data.
        /// </summary>
        /// <param name="request">Login request containing username and password.</param>
        /// <response code="200">Returns authentication data (tokens).</response>
        /// <response code="400">Invalid request model.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginReqDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid login request.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
            var response = await _authService.LoginAsync(request);
            if (!response.Succeeded)
            {
                var errors = response.Errors.Select(e => e.Description).ToList();
                return Unauthorized(new { message = "Login failed.", errors });
            }
            return Ok(new { message = "Login successful.", auth = response.AuthData });
        }

        /// <summary>
        /// Refresh authentication token using refresh token present in cookies.
        /// </summary>
        /// <response code="200">Returns refreshed authentication data.</response>
        /// <response code="401">Invalid or expired refresh token.</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            var response = await _authService.RefreshTokenAsync();
            if (!response.Succeeded)
            {
                var errors = response.Errors.Select(e => e.Description).ToList();
                return Unauthorized(new { message = "Refresh token failed.", errors });
            }
            return Ok(new { message = "Token refreshed successfully.", auth = response.AuthData });
        }

        /// <summary>
        /// Logout current user and revoke refresh token.
        /// </summary>
        /// <response code="200">Logout succeeded.</response>
        /// <response code="401">User not authenticated.</response>
        /// <response code="400">Logout failed.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userId = User?.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User is not authenticated." });
            var result = await _authService.LogoutAsync(userId);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// Revoke refresh token for a specific user.
        /// </summary>
        /// <param name="userId">User identifier whose token should be revoked.</param>
        /// <response code="200">Token revoked successfully.</response>
        /// <response code="400">Invalid user id supplied.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="404">User not found.</response>
        [HttpPost("revoke-token/{userId}")]
        [Authorize]
        [PermissionFilterFactory(Permission.Auth.RevokeToken)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeToken([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { message = "UserId is required." });
            var result = await _authService.RevokeTokenAsync(userId);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return NotFound(errors);
            }
            return Ok(new { message = result.Message });
        }
    }
}
