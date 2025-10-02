using Microsoft.AspNetCore.Mvc;
using BankingSystemAPI.Domain.Common;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Base controller providing consistent Result pattern handling
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Handle Result<T> responses with consistent HTTP status codes
        /// </summary>
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            return BadRequest(new 
            { 
                success = false,
                errors = result.Errors,
                message = result.ErrorMessage 
            });
        }

        /// <summary>
        /// Handle Result (non-generic) responses
        /// </summary>
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(new { success = true, message = "Operation completed successfully." });

            return BadRequest(new 
            { 
                success = false,
                errors = result.Errors,
                message = result.ErrorMessage 
            });
        }

        /// <summary>
        /// Handle Result<T> for creation scenarios (returns 201 Created)
        /// </summary>
        protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName = "", object? routeValues = null)
        {
            if (result.IsSuccess)
            {
                if (string.IsNullOrEmpty(actionName))
                    return CreatedAtAction(null, null, result.Value);
                
                return CreatedAtAction(actionName, routeValues, result.Value);
            }

            return BadRequest(new 
            { 
                success = false,
                errors = result.Errors,
                message = result.ErrorMessage 
            });
        }

        /// <summary>
        /// Handle Result<T> for update scenarios (returns 204 No Content on success)
        /// </summary>
        protected IActionResult HandleUpdateResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return NoContent();

            return BadRequest(new 
            { 
                success = false,
                errors = result.Errors,
                message = result.ErrorMessage 
            });
        }

        /// <summary>
        /// Handle Result for delete scenarios (returns 204 No Content on success)
        /// </summary>
        protected IActionResult HandleDeleteResult(Result result)
        {
            if (result.IsSuccess)
                return NoContent();

            return BadRequest(new 
            { 
                success = false,
                errors = result.Errors,
                message = result.ErrorMessage 
            });
        }
    }
}