using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    [Route("api/banks")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Banks")]
    public class BankController : ControllerBase
    {
        private readonly IBankService _bankService;

        public BankController(IBankService bankService)
        {
            _bankService = bankService;
        }

        [HttpGet]
        [PermissionFilterFactory(Permission.Bank.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var banks = await _bankService.GetAllAsync(pageNumber, pageSize);
            return Ok(new { message = "Banks retrieved successfully.", banks });
        }

        [HttpGet("{id:int}")]
        [PermissionFilterFactory(Permission.Bank.ReadById)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var bank = await _bankService.GetByIdAsync(id);
            if (bank == null)
                return NotFound(new { message = "Bank not found.", bank = (BankResDto?)null });
            return Ok(new { message = "Bank retrieved successfully.", bank });
        }

        [HttpGet("by-name/{name}")]
        [PermissionFilterFactory(Permission.Bank.ReadByName)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName(string name)
        {
            var bank = await _bankService.GetByNameAsync(name);
            if (bank == null)
                return NotFound(new { message = "Bank not found.", bank = (BankResDto?)null });
            return Ok(new { message = "Bank retrieved successfully.", bank });
        }

        [HttpPost]
        [PermissionFilterFactory(Permission.Bank.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] BankReqDto dto)
        {
            if (dto == null)
                return BadRequest("Bank data is required.");
            var created = await _bankService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { message = "Bank created successfully.", bank = created });
        }

        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.Bank.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] BankingSystemAPI.Application.DTOs.Bank.BankEditDto dto)
        {
            if (dto == null)
                return BadRequest("Bank data is required.");
            var updated = await _bankService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Bank not found." });
            return Ok(new { message = "Bank updated successfully.", bank = updated });
        }

        [HttpDelete("{id:int}")]
        [PermissionFilterFactory(Permission.Bank.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _bankService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Bank not found." });
            return Ok(new { message = "Bank deleted successfully." });
        }

        [HttpPut("{id:int}/active")]
        [PermissionFilterFactory(Permission.Bank.SetActive)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            await _bankService.SetBankActiveStatusAsync(id, isActive);
            return Ok(new { message = "Bank active status changed.", isActive });
        }
    }
}
