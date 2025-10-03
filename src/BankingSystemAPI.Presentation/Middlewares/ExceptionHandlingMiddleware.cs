using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Presentation.Helpers;

namespace BankingSystemAPI.Presentation.Middlewares
{
    /// <summary>
    /// Enhanced middleware to handle infrastructure and system-level exceptions with comprehensive ResultExtensions patterns.
    /// Optimized for .NET 8 with modern async patterns and performance improvements.
    /// Business logic errors should be handled via Result pattern in controllers.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        // Pre-compiled JSON serializer options for better performance
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = GenerateRequestId();
            SetRequestContext(context, requestId);

            try
            {
                await _next(context);
                
                // Use ResultExtensions for successful request logging with performance tracking
                var successResult = Result.Success();
                successResult.OnSuccess(() => 
                    _logger.LogDebug("[MIDDLEWARE] Request completed successfully: RequestId={RequestId}, Path={Path}, Method={Method}, StatusCode={StatusCode}", 
                        requestId, context.Request.Path, context.Request.Method, context.Response.StatusCode));
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, requestId);
            }
        }

        private static string GenerateRequestId()
        {
            // Use Span<T> for better performance in .NET 8
            Span<char> guidChars = stackalloc char[32];
            Guid.NewGuid().TryFormat(guidChars, out _, "N");
            return new string(guidChars[..16]); // Shorter, more readable request ID
        }

        private static void SetRequestContext(HttpContext context, string requestId)
        {
            context.Items["RequestId"] = requestId;
            
            // Use modern header manipulation for .NET 8
            if (!context.Response.Headers.ContainsKey("X-Request-ID"))
            {
                context.Response.Headers.Append("X-Request-ID", requestId);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            // Ensure response hasn't been started
            if (context.Response.HasStarted)
            {
                var startedResult = Result.BadRequest("Response has already started");
                startedResult.OnFailure(errors => 
                    _logger.LogWarning("[MIDDLEWARE] Cannot handle exception - response already started: RequestId={RequestId}", requestId));
                return;
            }

            // Set content type early for better performance
            context.Response.ContentType = "application/json; charset=utf-8";

            // Enhanced exception processing pipeline
            var exceptionProcessingResult = await ProcessExceptionAsync(context, exception, requestId);
            
            exceptionProcessingResult.OnSuccess(() => 
                _logger.LogDebug("[MIDDLEWARE] Exception processed successfully: RequestId={RequestId}", requestId))
                .OnFailure(errors => 
                _logger.LogError("[MIDDLEWARE] Failed to process exception: RequestId={RequestId}, Errors={Errors}", 
                    requestId, string.Join(", ", errors)));
        }

        private async Task<Result> ProcessExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            try
            {
                // Unwrap and categorize exception using functional approach
                var realException = GetInnermostException(exception);
                var categorizationResult = CategorizeException(realException);
                
                if (categorizationResult.IsFailure)
                    return Result.Failure(categorizationResult.Errors);

                var (statusCode, message, logLevel) = categorizationResult.Value;

                // Enhanced logging with comprehensive context
                LogException(realException, context, requestId, message, logLevel);

                // Set response status
                context.Response.StatusCode = statusCode;

                // Create and write error response
                var errorResponse = CreateErrorResponse(statusCode, message, requestId);
                var writeResult = await WriteErrorResponseAsync(context, errorResponse, requestId);
                
                return writeResult;
            }
            catch (Exception ex)
            {
                var processingError = Result.BadRequest($"Exception processing failed: {ex.Message}");
                processingError.OnFailure(errors => 
                    _logger.LogCritical(ex, "[MIDDLEWARE] Critical error in exception processing: RequestId={RequestId}", requestId));
                return processingError;
            }
        }

        private void LogException(Exception exception, HttpContext context, string requestId, string message, LogLevel logLevel)
        {
            var logResult = Result.Success();
            logResult.OnSuccess(() =>
            {
                const string logMessage = "[MIDDLEWARE] Exception handled: RequestId={RequestId}, Path={Path}, Method={Method}, ExceptionType={ExceptionType}, Message={Message}";
                var logArgs = new object[] { requestId, context.Request.Path, context.Request.Method, exception.GetType().Name, message };

                // Use pattern matching for cleaner code in .NET 8
                _ = logLevel switch
                {
                    LogLevel.Error => LogAndReturn(() => _logger.LogError(exception, logMessage, logArgs)),
                    LogLevel.Warning => LogAndReturn(() => _logger.LogWarning(exception, logMessage, logArgs)),
                    LogLevel.Information => LogAndReturn(() => _logger.LogInformation(exception, logMessage, logArgs)),
                    LogLevel.Critical => LogAndReturn(() => _logger.LogCritical(exception, logMessage, logArgs)),
                    _ => LogAndReturn(() => _logger.LogDebug(exception, logMessage, logArgs))
                };
            });

            // Local helper function for cleaner pattern matching
            static object LogAndReturn(Action logAction)
            {
                logAction();
                return new object();
            }
        }

        private Result<(int StatusCode, string Message, LogLevel LogLevel)> CategorizeException(Exception exception)
        {
            try
            {
                var result = exception switch
                {
                    // Database/Infrastructure exceptions
                    DbUpdateConcurrencyException => ((int)HttpStatusCode.Conflict, 
                        "A concurrency conflict occurred. Please refresh and try again.", LogLevel.Warning),
                    
                    DbUpdateException dbEx => ((int)HttpStatusCode.BadRequest, 
                        GetDatabaseErrorMessage(dbEx), LogLevel.Error),
                    
                    TimeoutException => ((int)HttpStatusCode.RequestTimeout, 
                        "The request timed out. Please try again.", LogLevel.Warning),
                    
                    // Authentication/Authorization exceptions
                    UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, 
                        "Access denied. Please authenticate and try again.", LogLevel.Warning),
                    
                    // Validation/Input exceptions  
                    ArgumentNullException argEx => ((int)HttpStatusCode.BadRequest, 
                        $"Invalid request parameters: {argEx.ParamName ?? "unknown"}", LogLevel.Warning),
                    
                    ArgumentException argEx => ((int)HttpStatusCode.BadRequest, 
                        $"Invalid request parameters: {argEx.ParamName ?? "unknown"}", LogLevel.Warning),
                    
                    InvalidOperationException => ((int)HttpStatusCode.BadRequest, 
                        "The requested operation is not valid in the current state.", LogLevel.Warning),
                    
                    // JSON/Serialization exceptions
                    JsonException => ((int)HttpStatusCode.BadRequest, 
                        "Invalid JSON format in request.", LogLevel.Warning),
                    
                    // System resource exceptions
                    OutOfMemoryException => ((int)HttpStatusCode.InternalServerError, 
                        "The system is experiencing high load. Please try again later.", LogLevel.Error),
                    
                    StackOverflowException => ((int)HttpStatusCode.InternalServerError, 
                        "A system error occurred. Please contact support.", LogLevel.Error),
                    
                    // Network/HTTP exceptions
                    HttpRequestException => ((int)HttpStatusCode.BadGateway, 
                        "External service unavailable. Please try again later.", LogLevel.Warning),
                    
                    TaskCanceledException => ((int)HttpStatusCode.RequestTimeout, 
                        "The request was cancelled or timed out.", LogLevel.Warning),
                    
                    OperationCanceledException => ((int)HttpStatusCode.RequestTimeout, 
                        "The operation was cancelled.", LogLevel.Warning),
                    
                    // File system exceptions
                    DirectoryNotFoundException => ((int)HttpStatusCode.InternalServerError, 
                        "A required resource was not found on the server.", LogLevel.Error),
                    
                    FileNotFoundException => ((int)HttpStatusCode.InternalServerError, 
                        "A required file was not found on the server.", LogLevel.Error),
                    
                    // Default for unhandled exceptions
                    _ => ((int)HttpStatusCode.InternalServerError, 
                        "An unexpected error occurred. Please try again or contact support if the problem persists.", LogLevel.Error)
                };

                return Result<(int StatusCode, string Message, LogLevel LogLevel)>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<(int StatusCode, string Message, LogLevel LogLevel)>.BadRequest($"Failed to categorize exception: {ex.Message}");
            }
        }

        private static string GetDatabaseErrorMessage(DbUpdateException dbEx)
        {
            // Enhanced database error message extraction with better performance
            var innerMessage = dbEx.InnerException?.Message ?? string.Empty;
            
            // Use ReadOnlySpan for better string operations in .NET 8
            ReadOnlySpan<char> messageSpan = innerMessage.AsSpan();
            
            if (messageSpan.Contains("UNIQUE constraint".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                messageSpan.Contains("duplicate key".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "A record with the same information already exists.";
            }
            
            if (messageSpan.Contains("FOREIGN KEY constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "The operation violates data relationships. Please check related records.";
            }

            if (messageSpan.Contains("CHECK constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "The operation violates data validation rules.";
            }

            if (messageSpan.Contains("NOT NULL constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return "Required information is missing. Please provide all required fields.";
            }

            return "A database error occurred while processing your request.";
        }

        private static ErrorDetails CreateErrorResponse(int statusCode, string message, string requestId)
        {
            return new ErrorDetails
            {
                Code = statusCode.ToString(),
                Message = message,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<Result> WriteErrorResponseAsync(HttpContext context, ErrorDetails error, string requestId)
        {
            try
            {
                // Use pre-compiled JSON options for better performance
                var payload = JsonSerializer.Serialize(error, JsonOptions);
                await context.Response.WriteAsync(payload);

                // Use ResultExtensions for successful response writing
                var responseResult = Result.Success();
                responseResult.OnSuccess(() => 
                    _logger.LogDebug("[MIDDLEWARE] Error response written successfully: RequestId={RequestId}, StatusCode={StatusCode}, Size={Size}", 
                        requestId, error.Code, payload.Length));
                
                return responseResult;
            }
            catch (Exception ex)
            {
                // Use ResultExtensions for response writing errors
                var errorResult = Result.BadRequest($"Failed to write error response: {ex.Message}");
                errorResult.OnFailure(errors => 
                    _logger.LogError(ex, "[MIDDLEWARE] Failed to write error response: RequestId={RequestId}", requestId));
                return errorResult;
            }
        }

        private static Exception GetInnermostException(Exception ex)
        {
            // Enhanced exception unwrapping with protection against circular references
            // Optimized for .NET 8 with better performance
            var current = ex;
            var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
            
            while (current.InnerException is not null && visited.Add(current))
            {
                current = current switch
                {
                    AggregateException { InnerExceptions.Count: > 0 } aex => aex.InnerExceptions[0],
                    _ => current.InnerException
                };
            }

            return current;
        }
    }
}
