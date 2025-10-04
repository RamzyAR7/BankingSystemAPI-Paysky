using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using BankingSystemAPI.Domain.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Unified endpoint to manage user role assignments.
    /// Provides a single PUT endpoint that removes old roles and assigns new ones.
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
        /// Update user role assignment (replaces old roles with new role)
        /// </summary>
        /// <param name="userId">The user ID to update roles for</param>
        /// <param name="updateDto">The new role assignment data</param>
        /// <returns>Success message indicating the role has been updated</returns>
        /// <response code="200">Role assignment updated successfully</response>
        /// <response code="400">Bad request - validation errors or invalid input</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="403">Forbidden - insufficient permissions</response>
        /// <response code="404">Not found - user does not exist</response>
        /// <response code="409">Conflict - business rule violation (e.g., cannot assign SuperAdmin role)</response>
        [HttpPut("{userId}")]
        [PermissionFilterFactory(Permission.UserRoles.Assign)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUserRole(
            [FromRoute] string userId, 
            [FromBody] UpdateUserRoleRequestDto updateDto)
        {
            var command = new UpdateUserRolesCommand(userId ?? string.Empty, updateDto?.Role ?? string.Empty);
            var result = await _mediator.Send(command);
            return HandleUpdateResult(result);
        }
    }
}
