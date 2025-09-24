using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.AuthorizationFilter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Controller that manages bank resources.
    /// </summary>
    /// <remarks>
    /// Exposes endpoints to perform CRUD operations on banks and to change their active status.
    /// All endpoints require authentication and appropriate permissions provided via the <c>PermissionFilterFactory</c>.
    /// </remarks>
    [Route("api/banks")]
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Banks")]
    public class BankController : ControllerBase
    {
        private readonly IBankService _bankService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BankController"/> class.
        /// </summary>
        /// <param name="bankService">Service used to manage banks.</param>
        public BankController(IBankService bankService)
        {
            _bankService = bankService;
        }

        /// <summary>
        /// Retrieves a paginated list of banks.
        /// </summary>
        /// <param name="pageNumber">Page number to retrieve. Defaults to 1.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
        /// <returns>
        /// 200 OK with a list of banks when successful.
        /// 401 Unauthorized if the caller is not authenticated.
        /// </returns>
        [HttpGet]
        [PermissionFilterFactory(Permission.Bank.ReadAll)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var banks = await _bankService.GetAllAsync(pageNumber, pageSize);
            return Ok(new { message = "Banks retrieved successfully.", banks });
        }

        /// <summary>
        /// Retrieves a bank by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the bank to retrieve.</param>
        /// <returns>
        /// 200 OK with the bank when found.
        /// 404 Not Found when no bank exists with the provided id.
        /// </returns>
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

        /// <summary>
        /// Retrieves a bank by its name.
        /// </summary>
        /// <param name="name">The name of the bank to retrieve.</param>
        /// <returns>
        /// 200 OK with the bank when found.
        /// 404 Not Found when no bank exists with the provided name.
        /// </returns>
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

        /// <summary>
        /// Creates a new bank.
        /// </summary>
        /// <param name="dto">The bank data to create.</param>
        /// <returns>
        /// 201 Created with the created bank when successful.
        /// 400 Bad Request when the provided data is null or invalid.
        /// </returns>
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

        /// <summary>
        /// Updates an existing bank.
        /// </summary>
        /// <param name="id">The identifier of the bank to update.</param>
        /// <param name="dto">The updated bank data.</param>
        /// <returns>
        /// 200 OK with the updated bank when successful.
        /// 400 Bad Request when the provided data is null or invalid.
        /// 404 Not Found when no bank exists with the provided id.
        /// </returns>
        [HttpPut("{id:int}")]
        [PermissionFilterFactory(Permission.Bank.Update)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] BankEditDto dto)
        {
            if (dto == null)
                return BadRequest("Bank data is required.");
            var updated = await _bankService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Bank not found." });
            return Ok(new { message = "Bank updated successfully.", bank = updated });
        }

        /// <summary>
        /// Deletes a bank by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the bank to delete.</param>
        /// <returns>
        /// 200 OK when the bank was successfully deleted.
        /// 404 Not Found when no bank exists with the provided id.
        /// </returns>
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

        /// <summary>
        /// Sets the active status of a bank.
        /// </summary>
        /// <param name="id">The identifier of the bank to modify.</param>
        /// <param name="isActive">Boolean flag indicating whether the bank should be active.</param>
        /// <returns>
        /// 200 OK when the active status change request is processed.
        /// 404 Not Found may be returned by the implementation if the bank does not exist.
        /// </returns>
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
