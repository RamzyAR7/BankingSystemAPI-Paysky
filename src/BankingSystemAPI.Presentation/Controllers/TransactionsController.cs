using BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId;
using BankingSystemAPI.Application.Features.Transactions.Queries.GetById;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Operations to read transaction history and list transactions.
    /// </summary>
    [Route("api/transactions")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Transactions")]
    public class TransactionsController : BaseApiController
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTransactionHistory(int accountId, int pageNumber = 1, int pageSize = 20, string? orderBy = null, string? orderDirection = null)
        {
            var result = await _mediator.Send(new GetTransactionsByAccountQuery(accountId, pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
        }

        /// <summary>
        /// Get transaction by id.
        /// </summary>
        [HttpGet("{transactionId:int}")]
        [PermissionFilterFactory(Permission.Transaction.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int transactionId)
        {
            var result = await _mediator.Send(new GetTransactionByIdQuery(transactionId));
            return HandleResult(result);
        }

        /// <summary>
        /// Get paginated list of all transactions.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.Transaction.ReadAllHistory)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 20, string? orderBy = null, string? orderDirection = null)
        {
            var result = await _mediator.Send(new GetAllTransactionsQuery(pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
        }
    }
}
