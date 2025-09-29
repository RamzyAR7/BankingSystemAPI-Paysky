using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using BankingSystemAPI.Presentation.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage savings accounts.
    /// </summary>
    [Route("api/savings-accounts")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "SavingsAccounts")]
    public class SavingsAccountController : ControllerBase
    {
        private readonly ISavingsAccountService _savingsAccountService;
        private readonly IAccountService _accountService;

        public SavingsAccountController(ISavingsAccountService savingsAccountService, IAccountService accountService)
        {
            _savingsAccountService = savingsAccountService;
            _accountService = accountService;
        }

        /// <summary>
        /// Get paginated list of savings accounts.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string? orderBy = null, string? orderDirection = null)
        {
            var allowed = new[] { "Id", "AccountNumber", "Balance", "CreatedDate" };
            if (!OrderByValidator.IsValid(orderBy, allowed))
                return BadRequest($"Invalid orderBy value. Allowed: {string.Join(',', allowed)}");

            var accounts = await _savingsAccountService.GetAccountsAsync(pageNumber, pageSize, orderBy, orderDirection);
            return Ok(new { message = "Savings accounts retrieved successfully.", accounts });
        }

        /// <summary>
        /// Create a new savings account.
        /// </summary>
        /// <remarks>
        /// Example request body:
        /// {
        ///   "userId": "string",
        ///   "currencyId": 1,
        ///   "initialDeposit": 100.00,
        ///   "interestType": 1 // 1=Monthly, 2=Quarterly, 3=Annually, 4=every5minutes (testing)
        /// }
        /// </remarks>
        [HttpPost]
        [PermissionFilterFactory(Permission.SavingsAccount.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] SavingsAccountReqDto reqDto)
        {
            var newAccount = await _savingsAccountService.CreateAccountAsync(reqDto);
            return CreatedAtAction(nameof(GetAll), new { id = newAccount.Id }, new { message = "Savings account created successfully.", account = newAccount });
        }

        /// <summary>
        /// Update an existing savings account.
        /// </summary>
        /// <remarks>
        /// Example request body:
        /// {
        ///   "currencyId": 1,
        ///   "isActive": true,
        ///   "interestType": 2 // 1=Monthly, 2=Quarterly, 3=Annually, 4=every5minutes (testing)
        /// }
        /// </remarks>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.SavingsAccount.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] SavingsAccountEditDto reqDto)
        {
            var updated = await _savingsAccountService.UpdateAccountAsync(id, reqDto);
            return Ok(new { message = "Savings account updated successfully.", account = updated });
        }

        /// <summary>
        /// Set account active/inactive.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.SavingsAccount.UpdateActiveStatus)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            await _accountService.SetAccountActiveStatusAsync(id, isActive);
            return Ok(new { message = $"Savings account active status changed to {isActive}." });
        }

        /// <summary>
        /// Get all interest logs with pagination.
        /// </summary>
        [HttpGet("interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadAllInterestRate)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllInterestLogs(int pageNumber = 1, int pageSize = 10)
        {
            var (logs, totalCount) = await _savingsAccountService.GetAllInterestLogsAsync(pageNumber, pageSize);
            return Ok(new { message = "Interest logs retrieved successfully.", totalCount, logs });
        }

        /// <summary>
        /// Get all interest logs for a specific account with pagination.
        /// </summary>
        [HttpGet("{accountId:int}/interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadInterestRateById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInterestLogsByAccountId(int accountId, int pageNumber = 1, int pageSize = 10)
        {
            var (logs, totalCount) = await _savingsAccountService.GetInterestLogsByAccountIdAsync(accountId, pageNumber, pageSize);
            return Ok(new { message = "Interest logs for account retrieved successfully.", totalCount, logs });
        }
    }
}
