#region Usings
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount;
using BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllInterestLogs;
using BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetInterestLogsByAccountId;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#endregion


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
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        private readonly IMediator _mediator;

        public SavingsAccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

    /// <summary>
    /// Get paginated list of savings accounts.
    /// </summary>
    /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
    /// <param name="orderBy">Optional. Property name to sort by. Common values: "Id", "AccountNumber", "UserId", "CreatedDate". If omitted the implementation default ordering is used.</param>
    /// <param name="orderDirection">Optional. Sort direction: "ASC" or "DESC" (case-insensitive). Defaults to "ASC".</param>
        [HttpGet]
        [PermissionFilterFactory(Permission.SavingsAccount.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string? orderBy = null, string? orderDirection = null)
        {
            var result = await _mediator.Send(new GetAllSavingsAccountsQuery(pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new savings account.
        /// </summary>
        /// The handler will validate that the currency and user exist and are active and will persist
        /// the new savings account. An account number and CreatedDate are auto-generated.
        /// <remarks>
        /// Currencies (id => code):
        /// - 1 => USD
        /// - 2 => EUR
        /// - 3 => GBP
        /// - 4 => EGP
        /// - 5 => SAR
        ///
        /// Interest Types (value => name):
        /// - 1 => Monthly (Interest calculated monthly)
        /// - 2 => Quarterly (Interest calculated quarterly)
        /// - 3 => Annually (Interest calculated annually)
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] SavingsAccountEditDto req)
        {
            var result = await _mediator.Send(new UpdateSavingsAccountCommand(id, req));
            return HandleUpdateResult(result);
        }

    /// <summary>
    /// Get all interest logs with pagination.
    /// </summary>
    /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
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
    /// <param name="accountId">The account identifier whose interest logs are returned.</param>
    /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
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

