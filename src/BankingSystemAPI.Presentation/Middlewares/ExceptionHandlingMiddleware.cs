using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BankingSystemAPI.Presentation.Helpers;

namespace BankingSystemAPI.Presentation.Middlewares
{
    /// <summary>
    /// Middleware to handle infrastructure and system-level exceptions.
    /// Business logic errors should be handled via Result pattern.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;
            context.Response.Headers["X-Request-ID"] = requestId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, requestId);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            context.Response.ContentType = "application/json";

            // Unwrap AggregateException / inner exceptions
            var realException = GetInnermostException(exception);

            // Log the exception for debugging
            _logger.LogError(realException, 
                "Unhandled exception occurred. RequestId: {RequestId}, Path: {Path}", 
                requestId, context.Request.Path);

            // Only handle infrastructure and system exceptions
            var (statusCode, message, logLevel) = MapException(realException);

            // Log with appropriate level
            if (logLevel == LogLevel.Error)
                _logger.LogError(realException, "Infrastructure error: {Message}", message);
            else
                _logger.LogWarning(realException, "Client error: {Message}", message);

            context.Response.StatusCode = statusCode;

            var error = new ErrorDetails
            {
                Code = statusCode.ToString(),
                Message = message,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var payload = JsonSerializer.Serialize(error, options);
            await context.Response.WriteAsync(payload);
        }

        private static (int StatusCode, string Message, LogLevel LogLevel) MapException(Exception exception)
        {
            return exception switch
            {
                // Infrastructure exceptions
                DbUpdateConcurrencyException => ((int)HttpStatusCode.Conflict, 
                    "A concurrency conflict occurred. Please refresh and try again.", LogLevel.Warning),
                
                DbUpdateException => ((int)HttpStatusCode.BadRequest, 
                    "A database error occurred while processing your request.", LogLevel.Error),
                
                TimeoutException => ((int)HttpStatusCode.RequestTimeout, 
                    "The request timed out. Please try again.", LogLevel.Warning),
                
                // System/Programming exceptions  
                ArgumentNullException => ((int)HttpStatusCode.BadRequest, 
                    "Invalid request parameters.", LogLevel.Warning),
                
                ArgumentException => ((int)HttpStatusCode.BadRequest, 
                    "Invalid request parameters.", LogLevel.Warning),
                
                InvalidOperationException => ((int)HttpStatusCode.BadRequest, 
                    "The requested operation is not valid in the current state.", LogLevel.Warning),
                
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, 
                    "Access denied. Please authenticate and try again.", LogLevel.Warning),
                
                // Generic system errors
                OutOfMemoryException => ((int)HttpStatusCode.InternalServerError, 
                    "The system is experiencing high load. Please try again later.", LogLevel.Error),
                
                StackOverflowException => ((int)HttpStatusCode.InternalServerError, 
                    "A system error occurred. Please contact support.", LogLevel.Error),
                
                // Default for unhandled exceptions
                _ => ((int)HttpStatusCode.InternalServerError, 
                    "An unexpected error occurred. Please try again or contact support if the problem persists.", LogLevel.Error)
            };
        }

        private static Exception GetInnermostException(Exception ex)
        {
            if (ex is AggregateException aex && aex.InnerExceptions != null && aex.InnerExceptions.Count > 0)
                return GetInnermostException(aex.InnerExceptions.First());

            return ex.InnerException ?? ex;
        }
    }
}
