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
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Currency")]
    public class CurrencyController : ControllerBase
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllCurrenciesQuery());
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Currencies retrieved successfully.", currencies = result.Value });
        }

        /// <summary>
        /// Get currency by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCurrencyByIdQuery(id));
            if (!result.Succeeded) return NotFound(new { message = result.Errors.FirstOrDefault() ?? "Currency not found.", currency = (CurrencyDto?)null });
            return Ok(new { message = "Currency retrieved successfully.", currency = result.Value });
        }

        /// <summary>
        /// Create a new currency.
        /// </summary>
        [HttpPost]
        [PermissionFilterFactory(Permission.Currency.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CurrencyReqDto reqDto)
        {
            var result = await _mediator.Send(new CreateCurrencyCommand(reqDto));
            if (!result.Succeeded) return BadRequest(result.Errors);
            var created = result.Value!;
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { message = "Currency created successfully.", currency = created });
        }

        /// <summary>
        /// Update an existing currency.
        /// </summary>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(int id, [FromBody] CurrencyReqDto reqDto)
        {
            var result = await _mediator.Send(new UpdateCurrencyCommand(id, reqDto));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Currency updated successfully.", currency = result.Value });
        }

        /// <summary>
        /// Delete a currency.
        /// </summary>
        [HttpDelete("{id:int}")]
        [PermissionFilterFactory(Permission.Currency.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteCurrencyCommand(id));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = "Currency deleted successfully." });
        }

        /// <summary>
        /// Set currency active/inactive.
        /// </summary>
        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.Currency.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            var result = await _mediator.Send(new SetCurrencyActiveStatusCommand(id, isActive));
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok(new { message = $"Currency active status changed to {isActive}." });
        }
    }
}
