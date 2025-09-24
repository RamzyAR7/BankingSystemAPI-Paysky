using BankingSystemAPI.Application.DTOs.Transactions;
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
    /// Operations to read transaction history and list transactions.
    /// </summary>
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
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
        public async Task<IActionResult> GetTransactionHistory(int accountId, int pageNumber = 1, int pageSize = 20)
        {
            var history = await _transactionService.GetByAccountIdAsync(accountId, pageNumber, pageSize);
            return Ok(new { message = "Transaction history retrieved successfully.", history });
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
            var trx = await _transactionService.GetByIdAsync(transactionId);
            if (trx == null)
                return NotFound(new { message = "Transaction not found.", transaction = (TransactionResDto?)null });
            return Ok(new { message = "Transaction retrieved successfully.", transaction = trx });
        }

        /// <summary>
        /// Get paginated list of all transactions.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.Transaction.ReadAllHistory)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllTransactions(int pageNumber = 1, int pageSize = 10)
        {
            var transactions = await _transactionService.GetAllAsync(pageNumber, pageSize);
            return Ok(new { message = "Transactions retrieved successfully.", transactions });
        }
    }
}
