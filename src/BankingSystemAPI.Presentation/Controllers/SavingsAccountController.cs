using BankingSystemAPI.Application.DTOs.Account;
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

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to manage savings accounts.
    /// </summary>
    [Route("api/savings-accounts")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "SavingsAccounts")]
    public class SavingsAccountController : BaseApiController
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string? orderBy = null, string? orderDirection = null)
        {
            var allowed = new[] { "Id", "AccountNumber", "Balance", "CreatedDate" };
            if (!OrderByValidator.IsValid(orderBy, allowed))
                return BadRequest(new { 
                    success = false, 
                    errors = new[] { $"Invalid orderBy value. Allowed: {string.Join(',', allowed)}" },
                    message = $"Invalid orderBy value. Allowed: {string.Join(',', allowed)}"
                });

            var result = await _mediator.Send(new GetAllSavingsAccountsQuery(pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
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
        public async Task<IActionResult> Create([FromBody] SavingsAccountReqDto req)
        {
            var result = await _mediator.Send(new CreateSavingsAccountCommand(req));
            return HandleCreatedResult(result, nameof(GetAll), new { id = result.Value?.Id });
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] SavingsAccountEditDto req)
        {
            var result = await _mediator.Send(new UpdateSavingsAccountCommand(id, req));
            return HandleUpdateResult(result);
        }

        /// <summary>
        /// Get all interest logs with pagination.
        /// </summary>
        [HttpGet("interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadAllInterestRate)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllInterestLogs(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _mediator.Send(new GetAllInterestLogsQuery(pageNumber, pageSize));
            return HandleResult(result);
        }

        /// <summary>
        /// Get all interest logs for a specific account with pagination.
        /// </summary>
        [HttpGet("{accountId:int}/interest-logs")]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadInterestRateById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInterestLogsByAccountId(int accountId, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _mediator.Send(new GetInterestLogsByAccountIdQuery(accountId, pageNumber, pageSize));
            return HandleResult(result);
        }
    }
}
