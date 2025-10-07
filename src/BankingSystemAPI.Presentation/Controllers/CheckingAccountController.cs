#region Usings
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount;
using BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts;
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
    /// Endpoints to manage checking accounts.
    /// </summary>
    [Route("api/checking-accounts")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "CheckingAccounts")]
    public class CheckingAccountController : BaseApiController
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

        public CheckingAccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get paginated list of checking accounts.
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
        /// <param name="orderBy">Optional. Property name to sort by. Common values: "Id", "AccountNumber", "UserId", "CreatedDate". If not specified a default order will be applied.</param>
        /// <param name="orderDirection">Optional. Sort direction: "ASC" or "DESC" (case-insensitive). Defaults to "ASC".</param>
        [HttpGet]
        [PermissionFilterFactory(Permission.CheckingAccount.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string? orderBy = null, string? orderDirection = null)
        {
            var result = await _mediator.Send(new GetAllCheckingAccountsQuery(pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new checking account.
        /// </summary>
        /// <remarks>
        /// Currencies (id => code):
        /// - 1 => USD
        /// - 2 => EUR
        /// - 3 => GBP
        /// - 4 => EGP
        /// - 5 => SAR
        /// </remarks>
        [HttpPost]
        [PermissionFilterFactory(Permission.CheckingAccount.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CheckingAccountReqDto req)
        {
            var result = await _mediator.Send(new CreateCheckingAccountCommand(req));
            return HandleCreatedResult(result, nameof(GetAll), new { id = result.Value?.Id });
        }

        /// <summary>
        /// Update an existing checking account.
        /// </summary>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.CheckingAccount.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CheckingAccountEditDto req)
        {
            var result = await _mediator.Send(new UpdateCheckingAccountCommand(id, req));
            return HandleUpdateResult(result);
        }
    }
}

