#region Usings
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUser;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword;
using BankingSystemAPI.Application.Features.Identity.Users.Commands.SetUserActiveStatus;
using BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers;
using BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserById;
using BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserByUsername;
using BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#endregion


namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// User management endpoints.
    /// </summary>
    [Route("api/users")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Users")]
    public class UserController : BaseApiController
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all users with pagination.
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
        /// <param name="orderBy">Optional. Property name to sort by. Common values: "Id", "UserName", "Email", "CreatedDate" (exact allowed properties depend on the backing entity). Invalid values may cause a Bad Request.</param>
        /// <param name="orderDirection">Optional. Sort direction: "ASC" or "DESC" (case-insensitive). Defaults to "ASC" when omitted.</param>
        [HttpGet]
        [PermissionFilterFactory(Permission.User.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10, string? orderBy = null, string? orderDirection = null)
        {
            var query = new GetAllUsersQuery(pageNumber, pageSize, orderBy, orderDirection);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get users by bank id.
        /// </summary>
        [HttpGet("by-bank/{bankId:int}")]
        [PermissionFilterFactory(Permission.User.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUsersByBankId(int bankId)
        {
            var query = new GetUsersByBankIdQuery(bankId);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by username.
        /// </summary>
        [HttpGet("by-username/{username}")]
        [PermissionFilterFactory(Permission.User.ReadByUsername)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var query = new GetUserByUsernameQuery(username);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by id.
        /// </summary>
        [HttpGet("{userId}")]
        [PermissionFilterFactory(Permission.User.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var query = new GetUserByIdQuery(userId);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new user.
        /// </summary>
        /// <remarks>
        /// Roles:
        /// - SuperAdmin
        /// - Admin
        /// - Client
        ///
        /// Banks (id => name):
        /// - 1 => National Bank of Egypt
        /// - 2 => Banque Misr
        /// - 3 => Commercial International Bank (CIB)
        /// - 4 => AlexBank
        /// </remarks>
        [HttpPost]
        [PermissionFilterFactory(Permission.User.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] UserReqDto user)
        {
            var command = new CreateUserCommand(user);
            var result = await _mediator.Send(command);
            return HandleCreatedResult(result, nameof(GetUserById), new { userId = result.Value?.Id });
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <remarks>
        /// Roles:
        /// - SuperAdmin
        /// - Admin
        /// - Client
        ///
        /// Banks (id => name):
        /// - 1 => National Bank of Egypt
        /// - 2 => Banque Misr
        /// - 3 => Commercial International Bank (CIB)
        /// - 4 => AlexBank
        ///
        /// Example request body:
        /// {
        ///   "email": "testuser@example.com",
        ///   "username": "testuser",
        ///   "fullName": "Test User Updated",
        ///   "nationalId": "12345678901234",
        ///   "phoneNumber": "12345678901",
        ///   "dateOfBirth": "2000-01-01"
        /// }
        /// </remarks>
        [HttpPut("{userId}")]
        [PermissionFilterFactory(Permission.User.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UserEditDto user)
        {
            var command = new UpdateUserCommand(userId, user);
            var result = await _mediator.Send(command);
            return HandleUpdateResult(result);
        }

        /// <summary>
        /// Change a user's password.
        /// </summary>
        [HttpPut("{userId}/password")]
        [PermissionFilterFactory(Permission.User.ChangePassword)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeUserPassword([FromRoute] string userId, [FromBody] ChangePasswordReqDto dto)
        {
            var command = new ChangeUserPasswordCommand(userId, dto);
            var result = await _mediator.Send(command);
            return HandleUpdateResult(result);
        }

        /// <summary>
        /// Delete a user by id.
        /// </summary>
        [HttpDelete("{userId}")]
        [PermissionFilterFactory(Permission.User.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var command = new DeleteUserCommand(userId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete multiple users.
        /// </summary>
        [HttpDelete("bulk")]
        [PermissionFilterFactory(Permission.User.DeleteRange)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUsers([FromBody] IEnumerable<string> userIds)
        {
            var command = new DeleteUsersCommand(userIds);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Get current authenticated user's info.
        /// </summary>
        [HttpGet("me")]
        [PermissionFilterFactory(Permission.User.ReadSelf)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyInfo()
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new
                {
                    success = false,
                    errors = new[] { "User is not authenticated." },
                    message = "User is not authenticated."
                });

            var query = new GetUserByIdQuery(userId);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Set user active/inactive status.
        /// </summary>
        [HttpPut("{userId}/active")]
        [PermissionFilterFactory(Permission.User.UpdateActiveStatus)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetActive(string userId, [FromQuery] bool isActive)
        {
            var command = new SetUserActiveStatusCommand(userId, isActive);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
