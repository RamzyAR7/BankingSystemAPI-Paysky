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
    /// Endpoints to manage user roles assignment.
    /// </summary>
    [Route("api/userroles")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "UserRoles")]
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRolesService _userRolesService;

        public UserRolesController(IUserRolesService userRolesService)
        {
            _userRolesService = userRolesService;
        }

        /// <summary>
        /// Assign roles to a user.
        /// </summary>
        [HttpPost("Assign")]
        [PermissionFilterFactory(Permission.UserRoles.Assign)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest("Invalid request data. UserId and Role are required.");
            }

            var result = await _userRolesService.UpdateUserRolesAsync(dto);
            if (!result.Succeeded)
            {
                var errorDescriptions = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(errorDescriptions);
            }
            return Ok(result.UserRole);
        }
    }
}
