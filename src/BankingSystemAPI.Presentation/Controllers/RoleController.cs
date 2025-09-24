using BankingSystemAPI.Application.DTOs.Role;
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
    /// Role management endpoints.
    /// </summary>
    [Route("api/roles")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Roles")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Get all roles.
        /// </summary>
        /// <response code="200">Returns a list of roles.</response>
        /// <response code="404">No roles found.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpGet("GetAllRoles")]
        [PermissionFilterFactory(Permission.Role.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            if (roles == null || !roles.Any())
            {
                return NotFound(new { message = "No roles found.", roles = new List<RoleResDto>() });
            }
            return Ok(new { message = "Roles retrieved successfully.", roles });
        }

        /// <summary>
        /// Create a new role.
        /// </summary>
        /// <response code="200">Role created successfully.</response>
        /// <response code="400">Invalid role data.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPost("CreateRole")]
        [PermissionFilterFactory(Permission.Role.Create)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] RoleReqDto roleReqDto)
        {
            if (roleReqDto == null || string.IsNullOrWhiteSpace(roleReqDto.Name))
            {
                return BadRequest(new { message = "Role name cannot be null or empty." });
            }
            var result = await _roleService.CreateRoleAsync(roleReqDto);
            if (result.Succeeded)
            {
                return Ok(new { message = "Role created successfully.", role = result.Role });
            }
            var errorDescriptions = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { message = "Role creation failed.", errors = errorDescriptions });
        }

        /// <summary>
        /// Delete a role by id.
        /// </summary>
        /// <response code="200">Role deleted successfully.</response>
        /// <response code="400">Invalid role id.</response>
        /// <response code="404">Role not found.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpDelete("DeleteRole/{roleId}")]
        [PermissionFilterFactory(Permission.Role.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRole([FromRoute] string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return BadRequest(new { message = "Role ID cannot be null or empty." });
            }
            var result = await _roleService.DeleteRoleAsync(roleId);
            if (result.Succeeded)
            {
                return Ok(new { message = "Role deleted successfully.", role = result.Role });
            }
            var errorDescriptions = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { message = "Role deletion failed.", errors = errorDescriptions });
        }
    }
}
