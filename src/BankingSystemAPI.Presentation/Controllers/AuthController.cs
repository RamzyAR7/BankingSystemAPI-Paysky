#region Usings
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Features.Identity.Auth.Commands.Login;
using BankingSystemAPI.Application.Features.Identity.Auth.Commands.Logout;
using BankingSystemAPI.Application.Features.Identity.Auth.Commands.RefreshToken;
using BankingSystemAPI.Application.Features.Identity.Auth.Commands.RevokeToken;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
#endregion


namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Authentication endpoints (login, refresh, logout, revoke tokens).
    /// </summary>
    [Route("api/auth")]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class AuthController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Login with credentials and receive authentication data.
        /// </summary>
        /// <param name="request">Login request containing username and password.</param>
        /// <response code="200">Returns authentication data (tokens).</response>
        /// <response code="400">Invalid request model or credentials.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginReqDto request)
        {
            var command = new LoginCommand(request.Email!, request.Password!);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Refresh authentication token using refresh token present in cookies.
        /// </summary>
        /// <response code="200">Returns refreshed authentication data.</response>
        /// <response code="400">Invalid or expired refresh token.</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var command = new RefreshTokenCommand(refreshToken);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Logout current user and revoke refresh token.
        /// </summary>
        /// <response code="200">Logout succeeded.</response>
        /// <response code="400">Logout failed or user not authenticated.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout()
        {
            var userId = User?.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { "User is not authenticated." },
                    message = "User is not authenticated."
                });

            var command = new LogoutCommand(userId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Revoke refresh token for a specific user.
        /// </summary>
        /// <param name="userId">User identifier whose token should be revoked.</param>
        /// <response code="200">Token revoked successfully.</response>
        /// <response code="400">Invalid user id supplied or operation failed.</response>
        [HttpPost("revoke-token/{userId}")]
        [Authorize]
        [PermissionFilterFactory(Permission.Auth.RevokeToken)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeToken([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { "UserId is required." },
                    message = "UserId is required."
                });

            var command = new RevokeTokenCommand(userId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}

