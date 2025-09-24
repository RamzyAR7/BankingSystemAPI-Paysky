using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BankingSystemAPI.Presentation.Filters
{
    public class RequestResponseLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<RequestResponseLoggingFilter> _logger;

        public RequestResponseLoggingFilter(ILogger<RequestResponseLoggingFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var sw = Stopwatch.StartNew();

            // Extract user info
            var user = httpContext.User;
            var userId = user?.FindFirst("uid")?.Value ?? user?.Identity?.Name;
            var claims = user?.Claims?.Select(c => new { c.Type, c.Value }).ToList();
            var roles = user?.Claims?.Where(c => c.Type == "role").Select(c => c.Value).ToList();

            // Read request body if possible
            string requestBody = null;
            if (request.ContentLength > 0 && request.Body.CanSeek)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // Log request info
            var requestInfo = new
            {
                Scheme = request.Scheme,
                Host = request.Host.ToString(),
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                Method = request.Method,
                UserId = userId,
                Claims = claims,
                Roles = roles,
                IP = httpContext.Connection.RemoteIpAddress?.ToString(),
                Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                RouteValues = context.ActionDescriptor.RouteValues,
                ActionArguments = context.ActionArguments,
                Body = requestBody
            };

            var prettyRequest = JsonSerializer.Serialize(requestInfo, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Incoming request:\n{Request}", prettyRequest);

            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Request] {DateTime.UtcNow:u}");
                Console.WriteLine(prettyRequest);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }

            // Execute the action
            ActionExecutedContext executedContext = null;
            Exception exception = null;
            try
            {
                executedContext = await next();
            }
            catch (Exception ex)
            {
                exception = ex;
                sw.Stop();
            }

            sw.Stop();

            // Prepare response info
            int? statusCode = null;
            object resultValue = null;
            Dictionary<string, string> responseHeaders = null;
            string responseBody = null;
            string errorMessage = null;
            string errorStack = null;

            if (executedContext?.Result is ObjectResult objectResult)
            {
                statusCode = objectResult.StatusCode ?? httpContext.Response.StatusCode;
                resultValue = objectResult.Value;
                responseBody = JsonSerializer.Serialize(resultValue, new JsonSerializerOptions { WriteIndented = true });
            }
            else if (executedContext?.Result is StatusCodeResult statusCodeResult)
            {
                statusCode = statusCodeResult.StatusCode;
            }
            else
            {
                statusCode = httpContext.Response.StatusCode;
            }

            responseHeaders = httpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            // If exception occurred
            if (exception != null)
            {
                errorMessage = exception.Message;
                errorStack = exception.StackTrace;
            }

            // Try to extract username/roles for summary if present in resultValue
            string username = null;
            string resultRoles = null;
            if (resultValue != null)
            {
                var resultType = resultValue.GetType();
                var usernameProp = resultType.GetProperty("Username");
                var rolesProp = resultType.GetProperty("Roles");
                if (usernameProp != null)
                    username = usernameProp.GetValue(resultValue)?.ToString();
                if (rolesProp != null)
                {
                    var rolesVal = rolesProp.GetValue(resultValue);
                    if (rolesVal is IEnumerable<string> rstr)
                        resultRoles = string.Join(",", rstr);
                    else
                        resultRoles = rolesVal?.ToString();
                }
            }

            var responseInfo = new
            {
                StatusCode = statusCode,
                Headers = responseHeaders,
                Result = resultValue,
                Body = responseBody,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                Exception = exception != null ? new { errorMessage, errorStack } : null,
                Username = username,
                Roles = resultRoles ?? (roles != null ? string.Join(",", roles) : null)
            };

            var prettyResponse = JsonSerializer.Serialize(responseInfo, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Outgoing response:\n{Response}", prettyResponse);

            // Add summary line for key fields if available
            if (username != null || resultRoles != null || userId != null)
            {
                _logger.LogInformation("Response summary: StatusCode={StatusCode}, Username={Username}, Roles={Roles}, UserId={UserId}", statusCode, username ?? userId, resultRoles ?? (roles != null ? string.Join(",", roles) : null), userId);
            }
            if (exception != null)
            {
                _logger.LogError(exception, "Exception occurred during request execution: {Message}", errorMessage);
            }

            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Response] {DateTime.UtcNow:u}");
                Console.WriteLine(prettyResponse);
                if (username != null || resultRoles != null || userId != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Summary: StatusCode={statusCode}, Username={username ?? userId}, Roles={resultRoles ?? (roles != null ? string.Join(",", roles) : null)}, UserId={userId}");
                }
                if (exception != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Exception: {errorMessage}\n{errorStack}");
                }
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
