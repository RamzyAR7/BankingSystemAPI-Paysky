using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;

namespace BankingSystemAPI.Presentation.Controllers
{
    /// <summary>
    /// Base controller providing consistent Result pattern handling with proper HTTP status codes
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        private ILogger _logger;
        protected ILogger Logger => _logger ??= HttpContext.RequestServices.GetService<ILogger<BaseApiController>>();

        /// <summary>
        /// Handle Result<T> responses with proper HTTP status codes based on error type
        /// </summary>
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogInformation("Operation completed successfully. Controller: {Controller}, Action: {Action}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName);
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Operation failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? Ok(result.Value)
                    : CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result (non-generic) responses with proper HTTP status codes
        /// </summary>
        protected IActionResult HandleResult(Result result)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogInformation("Operation completed successfully. Controller: {Controller}, Action: {Action}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName);
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Operation failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? Ok(new { success = true, message = "Operation completed successfully." })
                    : CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Create appropriate error response based on error message patterns
        /// This maps business logic errors to proper HTTP status codes
        /// </summary>
        private IActionResult CreateErrorResponse(IReadOnlyList<string> errors)
        {
            if (!errors.Any())
                return BadRequest(new { success = false, message = "Unknown error occurred." });

            var errorMessage = string.Join("; ", errors);
            var firstError = errors.First().ToLowerInvariant();

            // Map business logic errors to proper HTTP status codes
            var statusCode = firstError switch
            {
                // Not Found scenarios (404)
                var msg when msg.Contains("not found") => 404,
                var msg when msg.Contains("does not exist") => 404,
                var msg when msg.Contains("no longer exists") => 404,
                
                // Unauthorized scenarios (401) - Authentication failures
                var msg when msg.Contains("not authenticated") => 401,
                var msg when msg.Contains("invalid credentials") => 401,
                var msg when msg.Contains("email or password is incorrect") => 401,
                var msg when msg.Contains("token expired") => 401,
                var msg when msg.Contains("invalid token") => 401,
                var msg when msg.Contains("token has expired") => 401,
                
                // Forbidden scenarios (403) - Authorization failures
                var msg when msg.Contains("access denied") => 403,
                var msg when msg.Contains("insufficient permissions") => 403,
                var msg when msg.Contains("not authorized") => 403,
                var msg when msg.Contains("forbidden") => 403,
                var msg when msg.Contains("permission") && msg.Contains("denied") => 403,
                
                // Conflict scenarios (409) - Business rule violations
                var msg when msg.Contains("already exists") => 409,
                var msg when msg.Contains("duplicate") => 409,
                var msg when msg.Contains("conflict") => 409,
                var msg when msg.Contains("insufficient funds") => 409,
                var msg when msg.Contains("balance") && msg.Contains("insufficient") => 409,
                var msg when msg.Contains("account is inactive") => 409,
                var msg when msg.Contains("account is locked") => 409,
                var msg when msg.Contains("account is closed") => 409,
                var msg when msg.Contains("account is suspended") => 409,
                var msg when msg.Contains("daily limit exceeded") => 409,
                var msg when msg.Contains("transaction limit") => 409,
                
                // Unprocessable Entity (422) - Business validation failures
                var msg when msg.Contains("invalid amount") => 422,
                var msg when msg.Contains("validation failed") => 422,
                var msg when msg.Contains("business rule") => 422,
                var msg when msg.Contains("constraint violation") => 422,
                var msg when msg.Contains("amount must be positive") => 422,
                var msg when msg.Contains("invalid transaction type") => 422,
                
                // Bad Request (400) - Input validation failures (default)
                _ => 400
            };

            var errorResponse = new 
            { 
                success = false,
                errors = errors,
                message = errorMessage 
            };

            return statusCode switch
            {
                401 => Unauthorized(errorResponse),
                403 => StatusCode(403, errorResponse), // Forbid() doesn't accept value
                404 => NotFound(errorResponse),
                409 => Conflict(errorResponse),
                422 => UnprocessableEntity(errorResponse),
                _ => BadRequest(errorResponse)
            };
        }

        /// <summary>
        /// Handle Result<T> for creation scenarios (returns 201 Created)
        /// </summary>
        protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName = "", object? routeValues = null)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogInformation("Resource created successfully. Controller: {Controller}, Action: {Action}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName);
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Resource creation failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? (string.IsNullOrEmpty(actionName)
                        ? CreatedAtAction(null, null, result.Value)
                        : CreatedAtAction(actionName, routeValues, result.Value))
                    : CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result<T> for update scenarios (returns 204 No Content on success)
        /// </summary>
        protected IActionResult HandleUpdateResult<T>(Result<T> result)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogInformation("Resource updated successfully. Controller: {Controller}, Action: {Action}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName);
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Resource update failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? NoContent()
                    : CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result for delete scenarios (returns 204 No Content on success)
        /// </summary>
        protected IActionResult HandleDeleteResult(Result result)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogInformation("Resource deleted successfully. Controller: {Controller}, Action: {Action}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName);
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Resource deletion failed. Controller: {Controller}, Action: {Action}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? NoContent()
                    : CreateErrorResponse(result.Errors);
        }

        /// <summary>
        /// Handle Result<T> for query scenarios with additional context logging
        /// </summary>
        protected IActionResult HandleQueryResult<T>(Result<T> result, string? queryContext = null)
        {
            return result
                .OnSuccess(() => 
                {
                    Logger?.LogDebug("Query executed successfully. Controller: {Controller}, Action: {Action}, Context: {Context}", 
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        queryContext ?? "General query");
                })
                .OnFailure(errors => 
                {
                    Logger?.LogWarning("Query execution failed. Controller: {Controller}, Action: {Action}, Context: {Context}, Errors: {Errors}",
                        ControllerContext.ActionDescriptor.ControllerName,
                        ControllerContext.ActionDescriptor.ActionName,
                        queryContext ?? "General query",
                        string.Join(", ", errors));
                })
                .IsSuccess
                    ? Ok(result.Value)
                    : CreateErrorResponse(result.Errors);
        }
    }
}