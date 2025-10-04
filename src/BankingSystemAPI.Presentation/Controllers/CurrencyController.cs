using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency;
using BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus;
using BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetAllCurrencies;
using BankingSystemAPI.Application.Features.Currencies.Queries.GetCurrencyById;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Currency management endpoints.
    /// </summary>
    [Route("api/currency")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Currency")]
    public class CurrencyController : BaseApiController
    {
        private readonly IMediator _mediator;

        public CurrencyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all currencies.
        /// </summary>
        [HttpGet]
        [PermissionFilterFactory(Permission.Currency.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllCurrenciesQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Get currency by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCurrencyByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new currency.
        /// </summary>
        [HttpPost]
        [PermissionFilterFactory(Permission.Currency.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CurrencyReqDto reqDto)
        {
            var result = await _mediator.Send(new CreateCurrencyCommand(reqDto));
            return HandleCreatedResult(result, nameof(GetById), new { id = result.Value?.Id });
        }

        /// <summary>
        /// Update an existing currency.
        /// </summary>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CurrencyReqDto reqDto)
        {
            var result = await _mediator.Send(new UpdateCurrencyCommand(id, reqDto));
            // ✅ FIXED: Use HandleUpdateResult to return success message instead of full object
            return HandleUpdateResult(result);
        }

        /// <summary>
        /// Delete a currency.
        /// </summary>
        [HttpDelete("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteCurrencyCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Set currency active/inactive status.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.Currency.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            var result = await _mediator.Send(new SetCurrencyActiveStatusCommand(id, isActive));
            return HandleResult(result);
        }
    }
}
