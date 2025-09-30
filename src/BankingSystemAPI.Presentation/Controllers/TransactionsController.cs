using BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetById;
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
    /// Operations to read transaction history and list transactions.
    /// </summary>
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get paginated transaction history for an account.
        /// </summary>
        /// <param name="accountId">Account identifier whose history will be returned.</param>
        [HttpGet("{accountId}/history")]
        [PermissionFilterFactory(Permission.Transaction.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTransactionHistory(int accountId, int pageNumber = 1, int pageSize = 20, string? orderBy = null, string? orderDirection = null)
        {
            var allowed = new[] { "Timestamp", "Amount", "Id" };
            if (!OrderByValidator.IsValid(orderBy, allowed))
                return BadRequest($"Invalid orderBy value. Allowed: {string.Join(',', allowed)}");

            var res = await _mediator.Send(new GetTransactionsByAccountQuery(accountId, pageNumber, pageSize, orderBy, orderDirection));
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Transaction history retrieved successfully.", history = res.Value });
        }

        /// <summary>
        /// Get transaction by id.
        /// </summary>
        [HttpGet("{transactionId:int}")]
        [PermissionFilterFactory(Permission.Transaction.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(int transactionId)
        {
            var res = await _mediator.Send(new GetTransactionByIdQuery(transactionId));
            if (!res.Succeeded) return NotFound(new { message = res.Errors.FirstOrDefault() ?? "Transaction not found.", transaction = (object?)null });
            return Ok(new { message = "Transaction retrieved successfully.", transaction = res.Value });
        }

        /// <summary>
        /// Get paginated list of all transactions.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.Transaction.ReadAllHistory)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 20, string? orderBy = null, string? orderDirection = null)
        {
            var res = await _mediator.Send(new GetAllTransactionsQuery(pageNumber, pageSize, orderBy, orderDirection));
            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Transactions retrieved successfully.", transactions = res.Value });
        }
    }
}
