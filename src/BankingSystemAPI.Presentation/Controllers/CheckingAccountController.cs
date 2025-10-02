using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount;
using BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount;
using BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts;
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
    /// Endpoints to manage checking accounts.
    /// </summary>
    [Route("api/checking-accounts")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "CheckingAccounts")]
    public class CheckingAccountController : BaseApiController
    {
        private readonly IMediator _mediator;

        public CheckingAccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get paginated list of checking accounts.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.CheckingAccount.ReadAll)]
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

            var result = await _mediator.Send(new GetAllCheckingAccountsQuery(pageNumber, pageSize, orderBy, orderDirection));
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new checking account.
        /// </summary>
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CheckingAccountEditDto req)
        {
            var result = await _mediator.Send(new UpdateCheckingAccountCommand(id, req));
            return HandleUpdateResult(result);
        }
    }
}
