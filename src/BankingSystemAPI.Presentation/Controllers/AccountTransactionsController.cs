using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Endpoints to perform transactions on accounts (deposit, withdraw, transfer) and read balance.
    /// </summary>
    [Route("api/accounts")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "AccountTransactions")]
    public class AccountTransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountTransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Deposit money into an account.
        /// </summary>
        /// <param name="request">Deposit request containing account id and amount.</param>
        /// <response code="200">Deposit completed successfully and transaction details returned.</response>
        /// <response code="400">Invalid request (validation error or insufficient data).</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden - insufficient permissions.</response>
        /// <response code="404">Account not found.</response>
        // POST /api/accounts/deposit
        [HttpPost("deposit")]
        [PermissionFilterFactory(Permission.Transaction.Deposit)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<object>> Deposit([FromBody] DepositReqDto request)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Invalid deposit request.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });

            var res = await _mediator.Send(new DepositCommand(request));
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Deposit completed successfully.", transaction = res.Value });
        }

        /// <summary>
        /// Withdraw money from an account.
        /// </summary>
        /// <param name="request">Withdraw request containing account id and amount.</param>
        /// <response code="200">Withdrawal completed successfully and transaction details returned.</response>
        /// <response code="400">Invalid request or insufficient funds.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden - insufficient permissions.</response>
        /// <response code="404">Account not found.</response>
        // POST /api/accounts/withdraw
        [HttpPost("withdraw")]
        [PermissionFilterFactory(Permission.Transaction.Withdraw)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<object>> Withdraw([FromBody] WithdrawReqDto request)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Invalid withdraw request.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });

            var res = await _mediator.Send(new WithdrawCommand(request));
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Withdrawal completed successfully.", transaction = res.Value });
        }

        /// <summary>
        /// Transfer money between accounts.
        /// </summary>
        /// <param name="request">Transfer request containing source, target and amount.</param>
        /// <response code="200">Transfer completed successfully and transaction details returned.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden - insufficient permissions.</response>
        /// <response code="404">Source or target account not found.</response>
        /// <response code="409">Conflict (e.g., concurrent update or insufficient funds).</response>
        // POST /api/accounts/transfer
        [HttpPost("transfer")]
        [PermissionFilterFactory(Permission.Transaction.Transfer)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<object>> Transfer([FromBody] TransferReqDto request)
        {
            if (!ModelState.IsValid) return BadRequest(new { message = "Invalid transfer request.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });

            var res = await _mediator.Send(new TransferCommand(request));
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Transfer completed successfully.", transaction = res.Value });
        }

        /// <summary>
        /// Get current balance of an account.
        /// </summary>
        /// <param name="id">Account identifier.</param>
        /// <response code="200">Returns the current balance.</response>
        /// <response code="400">Invalid account id.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden - insufficient permissions.</response>
        /// <response code="404">Account not found.</response>
        // GET /api/accounts/{id}/balance
        [HttpGet("{accountId}/balance")]
        [PermissionFilterFactory(Permission.Transaction.ReadBalance)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            var res = await _mediator.Send(new GetBalanceQuery(accountId));
            if (!res.Succeeded) return NotFound(new { message = res.Errors.FirstOrDefault() ?? "Account not found." });
            return Ok(new { message = "Balance retrieved successfully.", balance = res.Value });
        }
    }
}
