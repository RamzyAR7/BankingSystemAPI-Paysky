using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage user roles assignment.
    /// </summary>
    [Route("api/user-roles")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "UserRoles")]
    public class UserRolesController : BaseApiController
    {
        private readonly IMediator _mediator;

        public UserRolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Update user role assignment.
        /// </summary>
        /// <param name="userId">The user ID to update</param>
        /// <param name="updateDto">The role update data</param>
        /// <returns>Result of the role update operation</returns>
        [HttpPut("{userId}")]
        [PermissionFilterFactory(Permission.UserRoles.Assign)]
        [ProducesResponseType(typeof(UserRoleUpdateResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRole(
            [FromRoute] string userId, 
            [FromBody] UpdateUserRoleRequestDto updateDto)
        {
            var command = new UpdateUserRolesCommand(userId ?? string.Empty, updateDto?.Role ?? string.Empty);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Assign roles to a user.
        /// </summary>
        /// <param name="dto">The user role assignment data</param>
        /// <returns>Result of the role assignment operation</returns>
        [HttpPost("Assign")]
        [PermissionFilterFactory(Permission.UserRoles.Assign)]
        [ProducesResponseType(typeof(UserRoleUpdateResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesDto dto)
        {
            var command = new UpdateUserRolesCommand(dto?.UserId ?? string.Empty, dto?.Role ?? string.Empty);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
