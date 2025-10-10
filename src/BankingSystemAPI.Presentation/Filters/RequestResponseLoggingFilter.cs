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
            var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;
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

            // Filter out sensitive headers
            var safeHeaders = request.Headers
                .Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(h.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => MaskSensitiveData(h.Value.ToString()));

            // Format and log the request
            var formattedRequest = $@"
───────────────────────────────
# [INCOMING REQUEST]
───────────────────────────────
-> Path:        {request.Method} {request.Path}
-> Query:       {request.QueryString}
-> UserId:      {userId ?? "Anonymous"}
-> Authenticated: {isAuthenticated}
-> Roles:       {(roles != null ? string.Join(",", roles) : "None")}
-> IP:          {httpContext.Connection.RemoteIpAddress}
-> Headers:     {JsonSerializer.Serialize(safeHeaders, new JsonSerializerOptions { WriteIndented = true })}
-> Body:        {MaskSensitiveData(requestBody)}
───────────────────────────────";

            PrintColored(formattedRequest, ConsoleColor.Cyan);

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

            int statusCode = executedContext?.HttpContext.Response.StatusCode ?? httpContext.Response.StatusCode;
            object? resultValue = (executedContext?.Result as ObjectResult)?.Value;
            var responseHeaders = httpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            string? responseBody = null;
            if (resultValue != null)
                responseBody = JsonSerializer.Serialize(resultValue, new JsonSerializerOptions { WriteIndented = true });

            // Format and log the response
            var formattedResponse = $@"
───────────────────────────────
# [OUTGOING RESPONSE]
───────────────────────────────
-> Status:      {statusCode}
-> Duration:    {sw.ElapsedMilliseconds} ms
-> UserId:      {userId ?? "Anonymous"}
-> Authenticated: {isAuthenticated}
-> Headers:     {JsonSerializer.Serialize(responseHeaders, new JsonSerializerOptions { WriteIndented = true })}
-> Body:        {MaskSensitiveData(responseBody)}
{(exception != null ? $"❌ Exception: {exception.Message}\n{exception.StackTrace}" : "")}
───────────────────────────────";

            PrintColored(formattedResponse, ConsoleColor.Green);

            // Extra debug-level logs (optional)
            _logger.LogDebug("Full Request Payload:\n{Request}", requestBody);
            _logger.LogDebug("Full Response Payload:\n{Response}", responseBody);

            if (exception != null)
                _logger.LogError(exception, "Unhandled exception occurred during request processing");
        }

        // Helper to mask sensitive values
        private static string? MaskSensitiveData(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var lowered = input.ToLowerInvariant();
            if (lowered.Contains("password") || lowered.Contains("token") ||
                lowered.Contains("authorization") || lowered.Contains("bearer"))
                return "***MASKED***";

            return input.Length > 800 ? input.Substring(0, 800) + "..." : input;
        }

        // Helper to print colored output in console (local dev)
        private static void PrintColored(string text, ConsoleColor color)
        {

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = old;
            }

        }
    }
}
