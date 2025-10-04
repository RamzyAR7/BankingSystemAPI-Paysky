using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount;
using BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts;
using BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId;
using BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Account management endpoints.
    /// </summary>
    [Route("api/accounts")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Accounts")]
    public class AccountController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get account by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [PermissionFilterFactory(Permission.Account.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetAccountByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Get account by account number.
        /// </summary>
        [HttpGet("by-number/{accountNumber}")]
        [PermissionFilterFactory(Permission.Account.ReadByAccountNumber)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByAccountNumber(string accountNumber)
        {
            var result = await _mediator.Send(new GetAccountByAccountNumberQuery(accountNumber));
            return HandleResult(result);
        }

        /// <summary>
        /// Get all accounts for a user.
        /// </summary>
        [HttpGet("by-user/{userId}")]
        [PermissionFilterFactory(Permission.Account.ReadByUserId)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _mediator.Send(new GetAccountsByUserIdQuery(userId));
            return HandleResult(result);
        }

        /// <summary>
        /// Get accounts by national ID.
        /// </summary>
        [HttpGet("by-national-id/{nationalId}")]
        [PermissionFilterFactory(Permission.Account.ReadByNationalId)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByNationalId(string nationalId)
        {
            var result = await _mediator.Send(new GetAccountsByNationalIdQuery(nationalId));
            return HandleResult(result);
        }

        /// <summary>
        /// Delete account by id.
        /// </summary>
        [HttpDelete("{id:int}")]
        [PermissionFilterFactory(Permission.Account.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteAccountCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Delete multiple accounts.
        /// </summary>
        [HttpDelete("bulk")]
        [PermissionFilterFactory(Permission.Account.DeleteMany)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMany([FromBody] IEnumerable<int> ids)
        {
            var result = await _mediator.Send(new DeleteAccountsCommand(ids));
            return HandleResult(result);
        }

        /// <summary>
        /// Set account active/inactive status.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.Account.UpdateActiveStatus)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            var result = await _mediator.Send(new SetAccountActiveStatusCommand(id, isActive));
            return HandleResult(result);
        }
    }
}
