#region Usings
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole;
using BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole;
using BankingSystemAPI.Application.Features.Identity.Roles.Queries.GetAllRoles;
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
    /// Role management endpoints.
    /// </summary>
    [Route("api/roles")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Roles")]
    public class RoleController : BaseApiController
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        private readonly IMediator _mediator;

        public RoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

    /// <summary>
    /// Get all roles.
    /// </summary>
    /// <remarks>
    /// This endpoint returns all roles. It does not accept ordering query parameters; a default ordering
    /// is applied by the backend. If you need ordering support, please request the feature or use the
    /// API's pagination to control result sets.
    /// </remarks>
        [HttpGet("GetAllRoles")]
        [PermissionFilterFactory(Permission.Role.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllRoles()
        {
            var query = new GetAllRolesQuery();
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new role.
        /// </summary>
        [HttpPost("CreateRole")]
        [PermissionFilterFactory(Permission.Role.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRole([FromBody] RoleReqDto roleReqDto)
        {
            if (roleReqDto == null || string.IsNullOrWhiteSpace(roleReqDto.Name))
            {
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { "Role name cannot be null or empty." },
                    message = "Role name cannot be null or empty."
                });
            }

            var command = new CreateRoleCommand(roleReqDto.Name);
            var result = await _mediator.Send(command);
            return HandleCreatedResult(result);
        }

        /// <summary>
        /// Delete a role by id.
        /// </summary>
        [HttpDelete("DeleteRole/{roleId}")]
        [PermissionFilterFactory(Permission.Role.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteRole([FromRoute] string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { "Role ID cannot be null or empty." },
                    message = "Role ID cannot be null or empty."
                });
            }

            var command = new DeleteRoleCommand(roleId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}

