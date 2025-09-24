using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// User management endpoints.
    /// </summary>
    [Route("api/users")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users.
        /// </summary>
        /// <response code="200">Returns list of users.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpGet]
        [PermissionFilterFactory(Permission.User.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10)
        {
            var users = await _userService.GetAllUsersAsync(pageNumber, pageSize);
            if (users == null || !users.Any())
            {
                return NotFound("No users found.");
            }
            return Ok(users);
        }

        /// <summary>
        /// Get users by bank id.
        /// SuperAdmin can view all users regardless of bankId; non-super-admins are restricted to their own bank; clients get empty list.
        /// </summary>
        [HttpGet("by-bank/{bankId:int}")]
        [PermissionFilterFactory(Permission.User.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsersByBankId(int bankId)
        {
            var users = await _userService.GetUsersByBankIdAsync(bankId);
            if (users == null || !users.Any())
                return NotFound("No users found for the specified bank.");
            return Ok(users);
        }

        /// <summary>
        /// Get users by bank name.
        /// SuperAdmin can view all users or filter by bank name; non-super-admins are restricted to their own bank; clients get empty list.
        /// </summary>
        [HttpGet("by-bank-name/{bankName}")]
        [PermissionFilterFactory(Permission.User.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsersByBankName(string bankName)
        {
            var users = await _userService.GetUsersByBankNameAsync(bankName);
            if (users == null || !users.Any())
                return NotFound("No users found for the specified bank name.");
            return Ok(users);
        }

        /// <summary>
        /// Get user by username.
        /// </summary>
        /// <response code="200">Returns the user.</response>
        /// <response code="404">User not found.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpGet("by-username/{username}")]
        [PermissionFilterFactory(Permission.User.ReadByUsername)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound($"User with username '{username}' not found.");
            }
            return Ok(user);
        }

        /// <summary>
        /// Get user by id.
        /// </summary>
        /// <response code="200">Returns the user.</response>
        /// <response code="404">User not found.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpGet("{userId}")]
        [PermissionFilterFactory(Permission.User.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID '{userId}' not found.");
            }
            return Ok(user);
        }

        /// <summary>
        /// Create a new user.
        /// </summary>
        /// <response code="201">User created.</response>
        /// <response code="400">Invalid user data.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPost]
        [PermissionFilterFactory(Permission.User.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateUser([FromBody] UserReqDto user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("User data and password are required.");
            }

            var result = await _userService.CreateUserAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }

            var createdUser = result.User;
            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = createdUser?.Id },
                new { message = "User created successfully.", user = createdUser }
            );
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <response code="200">User updated.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPut("{userId}")]
        [PermissionFilterFactory(Permission.User.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateUser([FromRoute]string userId, [FromBody] UserEditDto user)
        {
            if (string.IsNullOrWhiteSpace(userId) || user == null)
            {
                return BadRequest("User ID and user data are required.");
            }
            var result = await _userService.UpdateUserAsync(userId, user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            return Ok(result.User);
        }
        
        /// <summary>
        /// Change a user's password.
        /// </summary>
        /// <response code="200">Password changed.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPut("{userId}/password")]
        [PermissionFilterFactory(Permission.User.ChangePassword)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult>ChangeUserPassword([FromRoute]string userId, [FromBody] ChangePasswordReqDto dto)
        {
            if (string.IsNullOrWhiteSpace(userId) || dto == null || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest("User ID and new password are required.");
            }
            var result = await _userService.ChangeUserPasswordAsync(userId, dto);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            return Ok(result.User);
        }

        /// <summary>
        /// Delete a user by id.
        /// </summary>
        /// <response code="200">User deleted.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpDelete("{userId}")]
        [PermissionFilterFactory(Permission.User.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required.");
            }
            var result = await _userService.DeleteUserAsync(userId);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            return Ok(new { success = true, message = "User deleted successfully." });
        }

        /// <summary>
        /// Delete range of users.
        /// </summary>
        /// <response code="200">Users deleted.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpDelete("bulk")]
        [PermissionFilterFactory(Permission.User.DeleteRange)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteRangeOfUsers([FromBody] IEnumerable<string> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest("User IDs are required.");
            }
            var result = await _userService.DeleteRangeOfUsersAsync(userIds);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errors);
            }
            return Ok(new { success = true, message = "Users deleted successfully." });
        }

        /// <summary>
        /// Get current authenticated user's info.
        /// </summary>
        /// <response code="200">Returns the current user's info.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="404">User not found.</response>
        [HttpGet("me")]
        [PermissionFilterFactory(Permission.User.ReadSelf)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyInfo()
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            var user = await _userService.GetCurrentUserInfoAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        /// <summary>
        /// Set user active/inactive.
        /// </summary>
        [HttpPut("{userId}/active")]
        [PermissionFilterFactory(Permission.User.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetActive(string userId, [FromQuery] bool isActive)
        {
            await _userService.SetUserActiveStatusAsync(userId, isActive);
            return Ok(new { message = $"User active status changed to {isActive}." });
        }
    }
}