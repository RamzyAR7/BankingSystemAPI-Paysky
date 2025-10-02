using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage user roles assignment.
    /// </summary>
    [Route("api/userroles")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "UserRoles")]
    public class UserRolesController : BaseApiController
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
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { "Invalid request data. UserId and Role are required." },
                    message = "Invalid request data. UserId and Role are required."
                });
            }

            var result = await _userRolesService.UpdateUserRolesAsync(dto);
            return HandleResult(result);
        }
    }
}
