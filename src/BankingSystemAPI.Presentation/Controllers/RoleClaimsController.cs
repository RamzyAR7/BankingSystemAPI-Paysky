#region Usings
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#endregion


namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Role claims management endpoints.
    /// </summary>
    [Route("api/roleclaims")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "RoleClaims")]
    public class RoleClaimsController : BaseApiController
    {
        private readonly IRoleClaimsService _roleClaimsService;

        public RoleClaimsController(IRoleClaimsService roleClaimsService)
        {
            _roleClaimsService = roleClaimsService;
        }

        /// <summary>
        /// Update role claims for a role.
        /// </summary>
        [HttpPost("Assign")]
        [PermissionFilterFactory(Permission.RoleClaims.Assign)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateRoleClaims([FromBody] UpdateRoleClaimsDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RoleName) || dto.Claims == null)
            {
                var msg = ApiResponseMessages.Validation.RoleNameAndClaimsRequired;
                return BadRequest(new
                {
                    success = false,
                    errors = new[] { msg },
                    message = msg
                });
            }

            var result = await _roleClaimsService.UpdateRoleClaimsAsync(dto);
            return HandleResult(result);
        }

    /// <summary>
    /// Get all role claims grouped by category.
    /// </summary>
    /// <remarks>
    /// Returns role claims grouped by category. This endpoint does not accept ordering query parameters;
    /// the grouping order is implementation-defined.
    /// </remarks>
        [HttpGet("GetAllClaims")]
        [PermissionFilterFactory(Permission.RoleClaims.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllClaims()
        {
            var result = await _roleClaimsService.GetAllClaimsByGroup();
            return HandleResult(result);
        }
    }
}

