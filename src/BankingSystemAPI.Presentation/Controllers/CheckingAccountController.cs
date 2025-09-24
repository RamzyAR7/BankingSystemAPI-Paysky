using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage checking accounts.
    /// </summary>
    [Route("api/checking-accounts")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "CheckingAccounts")]
    public class CheckingAccountController : ControllerBase
    {
        private readonly IAccountTypeService<CheckingAccount, CheckingAccountReqDto, CheckingAccountEditDto, CheckingAccountDto> _checkingAccountService;
        private readonly IAccountService _accountService;

        public CheckingAccountController(IAccountTypeService<CheckingAccount, CheckingAccountReqDto, CheckingAccountEditDto, CheckingAccountDto> checkingAccountService, IAccountService accountService)
        {
            _checkingAccountService = checkingAccountService;
            _accountService = accountService;
        }

        /// <summary>
        /// Get paginated list of checking accounts.
        /// </summary>
        /// <response code="200">Returns a page of checking accounts.</response>
        /// <response code="400">Invalid paging parameters.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="400">Bad request.</response>
        [HttpGet]
        [PermissionFilterFactory(Permission.CheckingAccount.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var accounts = await _checkingAccountService.GetAccountsAsync(pageNumber, pageSize);
            return Ok(new { message = "Checking accounts retrieved successfully.", accounts });
        }

        /// <summary>
        /// Create a new checking account.
        /// </summary>
        /// <response code="201">Account created.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="409">Conflict.</response>
        [HttpPost]
        [PermissionFilterFactory(Permission.CheckingAccount.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CheckingAccountReqDto reqDto)
        {
            var newAccount = await _checkingAccountService.CreateAccountAsync(reqDto);
            return CreatedAtAction(nameof(GetAll), new { id = newAccount.Id }, new { message = "Checking account created successfully.", account = newAccount });
        }

        /// <summary>
        /// Update an existing checking account.
        /// </summary>
        /// <response code="200">Account updated.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Account not found.</response>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.CheckingAccount.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CheckingAccountEditDto reqDto)
        {
            var updated = await _checkingAccountService.UpdateAccountAsync(id, reqDto);
            return Ok(new { message = "Checking account updated successfully.", account = updated });
        }

        /// <summary>
        /// Set account active/inactive.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.CheckingAccount.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            await _accountService.SetAccountActiveStatusAsync(id, isActive);
            return Ok(new { message = $"Checking account active status changed to {isActive}." });
        }
    }
}
