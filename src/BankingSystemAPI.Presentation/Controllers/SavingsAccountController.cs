using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount;
using BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllInterestLogs;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetInterestLogsByAccountId;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using BankingSystemAPI.Presentation.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage savings accounts.
    /// Only: GetAll, Create, Update, GetAllInterestLogs, GetInterestLogsByAccountId
    /// </summary>
    [Route("api/savings-accounts")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "SavingsAccounts")]
    public class SavingsAccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SavingsAccountController(IMediator mediator)
        {
            _mediator = mediator;
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

            var result = await _mediator.Send(new GetAllSavingsAccountsQuery(pageNumber, pageSize, orderBy, orderDirection));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Savings accounts retrieved successfully.", accounts = result.Value });
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
        /// 
        /// The handler will validate that the currency and user exist and are active and will persist
        /// the new savings account. An account number and CreatedDate are auto-generated.
        /// </remarks>
        [HttpPost]
        [PermissionFilterFactory(Permission.SavingsAccount.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] SavingsAccountReqDto req)
        {
            var result = await _mediator.Send(new CreateSavingsAccountCommand(req));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return CreatedAtAction(nameof(GetAll), new { id = result.Value!.Id }, new { message = "Savings account created successfully.", account = result.Value });
        }

        /// <summary>
        /// Update an existing savings account.
        /// </summary>
        /// <remarks>
        /// Updates editable fields of a savings account such as currency and interest rate. The
        /// request must not attempt to change the account balance. InterestType values correspond
        /// to the InterestType enum and are used by the interest calculation job.
        /// </remarks>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.SavingsAccount.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] SavingsAccountEditDto req)
        {
            var result = await _mediator.Send(new UpdateSavingsAccountCommand(id, req));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Savings account updated successfully.", account = result.Value });
        }

        /// <summary>
        /// Get all interest logs with pagination.
        /// </summary>
        [HttpGet("interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadAllInterestRate)]
        public async Task<IActionResult> GetAllInterestLogs(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _mediator.Send(new GetAllInterestLogsQuery(pageNumber, pageSize));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Interest logs retrieved successfully.", totalCount = result.Value!.TotalCount, logs = result.Value.Logs });
        }

        /// <summary>
        /// Get all interest logs for a specific account with pagination.
        /// </summary>
        [HttpGet("{accountId:int}/interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadInterestRateById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInterestLogsByAccountId(int accountId, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _mediator.Send(new GetInterestLogsByAccountIdQuery(accountId, pageNumber, pageSize));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Interest logs for account retrieved successfully.", totalCount = result.Value!.TotalCount, logs = result.Value.Logs });
        }
    }
}
