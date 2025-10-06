#region Usings
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Presentation.Services;
#endregion


namespace BankingSystemAPI.Presentation.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        private ILogger? _logger;
        protected ILogger Logger => _logger ??= HttpContext.RequestServices.GetService<ILogger<BaseApiController>>()!;

        private IErrorResponseFactory ErrorFactory => HttpContext.RequestServices.GetService<IErrorResponseFactory>() ?? new ErrorResponseFactory(NullLogger<ErrorResponseFactory>.Instance);
        #region Handle of Result pattern
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();
                return Ok(result.Value);
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result);
        }
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();
                return Ok(new { success = true, message = GetSuccessMessage() });
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result);
        }

        protected IActionResult HandleUpdateResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                LogSuccess();

                var action = GetActionName();
                var controller = GetControllerName();

                var isPassword = action.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                 action.Contains("changepassword", StringComparison.OrdinalIgnoreCase);

                var isStatus = action.Contains("active", StringComparison.OrdinalIgnoreCase) ||
                               action.Contains("status", StringComparison.OrdinalIgnoreCase) ||
                               action.Contains("setactive", StringComparison.OrdinalIgnoreCase) ||
                               controller.Contains("active", StringComparison.OrdinalIgnoreCase);

                if (isPassword || isStatus)
                    return Ok(new { success = true, message = GetSuccessMessage() });

                return Ok(new { success = true, message = GetSuccessMessage(), data = result.Value });
            }

            LogFailure(result.Errors);
            return CreateErrorResponse(result);
        }

        protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName = "", object? routeValues = null)
        {
            if (!result.IsSuccess)
            {
                LogFailure(result.Errors);
                return CreateErrorResponse(result);
            }

            Logger.LogInformation("Resource created successfully. Controller: {Controller}, Action: {Action}", GetControllerName(), GetActionName());

            if (!string.IsNullOrWhiteSpace(actionName))
                return CreatedAtAction(actionName, routeValues, result.Value);

            return StatusCode(201, result.Value);
        }
        #endregion

        #region  Messages
        private string GetSuccessMessage()
        {
            try
            {
                var method = HttpContext.Request.Method;
                var controller = GetControllerName();
                var action = GetActionName();

                var provider = HttpContext.RequestServices.GetService<ISuccessMessageProvider>();
                return provider?.GetSuccessMessage(method, controller, action, HttpContext.Request.Query) ?? ApiResponseMessages.Generic.OperationCompleted;
            }
            catch
            {
                return ApiResponseMessages.Generic.OperationCompleted;
            }
        }
        #endregion

        #region Helpers
        private string GetControllerName() => ControllerContext.ActionDescriptor.ControllerName?.ToLowerInvariant() ?? string.Empty;
        private string GetActionName() => ControllerContext.ActionDescriptor.ActionName?.ToLowerInvariant() ?? string.Empty;

        private void LogSuccess() => Logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, GetControllerName(), GetActionName());
        private void LogFailure(IReadOnlyList<string> errors) => Logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, GetControllerName(), GetActionName(), string.Join(", ", errors));
        #endregion

        #region Error Response
        private IActionResult CreateErrorResponse(Result result)
        {
            var (statusCode, body) = ErrorFactory.Create(result.ErrorItems);

            return statusCode switch
            {
                401 => Unauthorized(body),
                403 => StatusCode(403, body),
                404 => NotFound(body),
                409 => Conflict(body),
                422 => UnprocessableEntity(body),
                _ => BadRequest(body)
            };
        }
        #endregion
    }
}
