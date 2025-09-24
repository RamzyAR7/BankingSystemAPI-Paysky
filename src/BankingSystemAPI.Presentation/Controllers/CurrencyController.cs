using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Permissions;

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
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
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
            var currencies = await _currencyService.GetAllAsync();
            return Ok(new { message = "Currencies retrieved successfully.", currencies });
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
            var currency = await _currencyService.GetByIdAsync(id);
            if (currency == null)
                return NotFound(new { message = "Currency not found.", currency = (CurrencyDto?)null });
            return Ok(new { message = "Currency retrieved successfully.", currency });
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
            var created = await _currencyService.CreateAsync(reqDto);
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
            var updated = await _currencyService.UpdateAsync(id, reqDto);
            return Ok(new { message = "Currency updated successfully.", currency = updated });
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
            await _currencyService.DeleteAsync(id);
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
            await _currencyService.SetCurrencyActiveStatusAsync(id, isActive);
            return Ok(new { message = $"Currency active status changed to {isActive}." });
        }
    }
}
