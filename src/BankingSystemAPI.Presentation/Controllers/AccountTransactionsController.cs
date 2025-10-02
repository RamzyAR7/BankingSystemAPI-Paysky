using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Features.Transactions.Commands.Deposit;
using BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw;
using BankingSystemAPI.Application.Features.Transactions.Commands.Transfer;
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
    [Authorize]
    [ApiExplorerSettings(GroupName = "AccountTransactions")]
    public class AccountTransactionsController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AccountTransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Deposit money into an account.
        /// </summary>
        [HttpPost("deposit")]
        [PermissionFilterFactory(Permission.Transaction.Deposit)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deposit([FromBody] DepositReqDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { 
                    success = false, 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray(),
                    message = "Invalid deposit request."
                });

            var result = await _mediator.Send(new DepositCommand(request));
            return HandleResult(result);
        }

        /// <summary>
        /// Withdraw money from an account.
        /// </summary>
        [HttpPost("withdraw")]
        [PermissionFilterFactory(Permission.Transaction.Withdraw)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawReqDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { 
                    success = false, 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray(),
                    message = "Invalid withdraw request."
                });

            var result = await _mediator.Send(new WithdrawCommand(request));
            return HandleResult(result);
        }

        /// <summary>
        /// Transfer money between accounts.
        /// </summary>
        [HttpPost("transfer")]
        [PermissionFilterFactory(Permission.Transaction.Transfer)]
        [EnableRateLimiting("MoneyPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Transfer([FromBody] TransferReqDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(new { 
                    success = false, 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray(),
                    message = "Invalid transfer request."
                });

            var result = await _mediator.Send(new TransferCommand(request));
            return HandleResult(result);
        }

        /// <summary>
        /// Get current balance of an account.
        /// </summary>
        [HttpGet("{accountId}/balance")]
        [PermissionFilterFactory(Permission.Transaction.ReadBalance)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            var result = await _mediator.Send(new GetBalanceQuery(accountId));
            return HandleResult(result);
        }
    }
}
