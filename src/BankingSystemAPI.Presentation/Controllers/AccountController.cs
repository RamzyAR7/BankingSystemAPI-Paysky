using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Account management endpoints.
    /// </summary>
    [Route("api/accounts")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Accounts")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Get account by id.
        /// </summary>
        /// <param name="id">Account identifier.</param>
        /// <returns>Account details.</returns>
        /// <response code="200">Returns the account details.</response>
        /// <response code="400">Invalid account id supplied.</response>
        /// <response code="404">Account not found.</response>
        /// <response code="401">Unauthorized.</response>
        // GET /api/account/{id}
        [HttpGet("{id:int}")]
        [PermissionFilterFactory(Permission.Account.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AccountDto>> GetById(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound(new { message = "Account not found.", account = (AccountDto?)null });
            return Ok(new { message = "Account retrieved successfully.", account });
        }

        /// <summary>
        /// Get account by account number.
        /// </summary>
        /// <param name="accountNumber">Account number to search by.</param>
        /// <returns>Account details when found.</returns>
        /// <response code="200">Returns the account details.</response>
        /// <response code="400">Invalid or missing account number.</response>
        /// <response code="404">Account not found.</response>
        /// <response code="401">Unauthorized.</response>
        // GET /api/account/by-number/{accountNumber}
        [HttpGet("by-number/{accountNumber}")]
        [PermissionFilterFactory(Permission.Account.ReadByAccountNumber)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AccountDto>> GetByAccountNumber(string accountNumber)
        {
            var account = await _accountService.GetAccountByAccountNumberAsync(accountNumber);
            if (account == null)
                return NotFound(new { message = "Account not found.", account = (AccountDto?)null });
            return Ok(new { message = "Account retrieved successfully.", account });
        }

        /// <summary>
        /// Get all accounts for a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>List of accounts for the user.</returns>
        /// <response code="200">Returns a list of accounts (may be empty).</response>
        /// <response code="400">Invalid user id supplied.</response>
        /// <response code="401">Unauthorized.</response>
        // GET /api/account/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        [PermissionFilterFactory(Permission.Account.ReadByUserId)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetByUserId(string userId)
        {
            var accounts = await _accountService.GetAccountsByUserIdAsync(userId);
            return Ok(new { message = "Accounts retrieved successfully.", accounts });
        }

        /// <summary>
        /// Get accounts by national ID.
        /// </summary>
        /// <param name="nationalId">National identifier.</param>
        /// <returns>Accounts matching the national ID.</returns>
        /// <response code="200">Returns accounts matching the national id.</response>
        /// <response code="400">Invalid national id supplied.</response>
        /// <response code="401">Unauthorized.</response>
        // GET /api/account/by-national-id/{nationalId}
        [HttpGet("by-national-id/{nationalId}")]
        [PermissionFilterFactory(Permission.Account.ReadByNationalId)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetByNationalId(string nationalId)
        {
            var accounts = await _accountService.GetAccountsByNationalIdAsync(nationalId);
            return Ok(new { message = "Accounts retrieved successfully.", accounts });
        }

        /// <summary>
        /// Delete account by id.
        /// </summary>
        /// <param name="id">Account identifier.</param>
        /// <response code="204">Account deleted successfully.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="404">Account not found.</response>
        /// <response code="401">Unauthorized.</response>
        // DELETE /api/account/{id}
        [HttpDelete("{id:int}")]
        [PermissionFilterFactory(Permission.Account.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Delete(int id)
        {
            await _accountService.DeleteAccountAsync(id);
            return Ok(new { message = "Account deleted successfully." });
        }

        /// <summary>
        /// Delete multiple accounts.
        /// </summary>
        /// <param name="ids">List of account ids to delete.</param>
        /// <response code="204">Accounts deleted successfully.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        // DELETE /api/account/bulk
        [HttpDelete("bulk")]
        [PermissionFilterFactory(Permission.Account.DeleteMany)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteMany([FromBody] IEnumerable<int> ids)
        {
            await _accountService.DeleteAccountsAsync(ids);
            return Ok(new { message = "Accounts deleted successfully." });
        }

        /// <summary>
        /// Set account active/inactive.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.Account.UpdateActiveStatus)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            await _accountService.SetAccountActiveStatusAsync(id, isActive);
            return Ok(new { message = $"Account active status changed to {isActive}." });
        }
    }
}
