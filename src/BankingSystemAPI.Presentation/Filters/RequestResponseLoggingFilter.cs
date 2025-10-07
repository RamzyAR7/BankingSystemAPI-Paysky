#region Usings
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
using BankingSystemAPI.Domain.Constant;
#endregion


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
            string? requestBody = null;
            if (request.ContentLength.GetValueOrDefault() > 0 && request.Body.CanSeek)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // Build request info
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

            var requestJson = JsonSerializer.Serialize(requestInfo, new JsonSerializerOptions { WriteIndented = true });

            // Log a concise info line and keep the full payload at Debug level
            _logger.LogInformation("Incoming request: {Method} {Path} UserId={UserId} Query={Query}", request.Method, request.Path, userId ?? "-", request.QueryString);
            _logger.LogDebug(ApiResponseMessages.Logging.IncomingRequest, requestJson);

            // Execute the action
            ActionExecutedContext? executedContext = null;
            Exception? exception = null;
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
            object? resultValue = null;
            Dictionary<string, string>? responseHeaders = null;
            string? responseBody = null;
            string? errorMessage = null;
            string? errorStack = null;

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
            string? username = null;
            string? resultRoles = null;
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

            var responseJson = JsonSerializer.Serialize(responseInfo, new JsonSerializerOptions { WriteIndented = true });

            // Log concise response summary at Information and full payload at Debug
            _logger.LogInformation("Outgoing response: {Method} {Path} StatusCode={Status} ElapsedMs={Elapsed} UserId={UserId}", request.Method, request.Path, statusCode, sw.ElapsedMilliseconds, userId ?? "-");
            _logger.LogDebug(ApiResponseMessages.Logging.OutgoingResponse, responseJson);

            if (username != null || resultRoles != null || userId != null)
            {
                _logger.LogInformation(ApiResponseMessages.Logging.ResponseSummary, statusCode, username ?? userId, resultRoles ?? (roles != null ? string.Join(",", roles) : null), userId);
            }

            if (exception != null)
            {
                _logger.LogError(exception, ApiResponseMessages.Logging.ExceptionOccurred + " - {Message}", errorMessage);
            }

            // Optional: print a simple colored diff to console when enabled via env var
            try
            {
                var enableDiff = Environment.GetEnvironmentVariable("ENABLE_CONSOLE_DIFF");
                if (!string.IsNullOrEmpty(enableDiff) && enableDiff.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Request/Response Diff] {DateTime.UtcNow:u} {request.Method} {request.Path} Status={statusCode}");
                    PrintColoredDiffLines(requestJson, responseJson);
                }
            }
            catch
            {
                // swallow any console-related errors to avoid affecting request pipeline
            }
        }

        // Simple line-based diff printer: lines only in request => red (-), only in response => green (+), common => default
        private static void PrintColoredDiffLines(string left, string right)
        {
            if (left == null) left = string.Empty;
            if (right == null) right = string.Empty;

            var leftLines = left.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
            var rightLines = right.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();

            var leftSet = new HashSet<string>(leftLines);
            var rightSet = new HashSet<string>(rightLines);

            // iterate through union preserving an order: first left lines then right-only lines
            foreach (var line in leftLines)
            {
                if (rightSet.Contains(line))
                {
                    Console.ResetColor();
                    Console.WriteLine("  " + line);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("- " + line);
                    Console.ResetColor();
                }
            }

            // print lines that are only in right and were not printed
            foreach (var line in rightLines)
            {
                if (!leftSet.Contains(line))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("+ " + line);
                    Console.ResetColor();
                }
            }
        }
    }
}

