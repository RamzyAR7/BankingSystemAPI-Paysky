#region Usings
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Presentation.Helpers;
using BankingSystemAPI.Domain.Constant;
using System.Text.Json.Serialization;
using Serilog.Context;
#endregion

namespace BankingSystemAPI.Presentation.Middlewares
{
    #region Class
    /// <summary>
    /// Enhanced middleware to handle infrastructure and system-level exceptions with comprehensive ResultExtensions patterns.
    /// Optimized for .NET 8 with modern async patterns and performance improvements.
    /// Business logic errors should be handled via Result pattern in controllers.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        #region Fields
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        // Pre-compiled JSON serializer options for better performance
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        #endregion

        #region Constructor
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = GenerateRequestId();
            SetRequestContext(context, requestId);

            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();
            using (LogContext.PushProperty(LoggingConstants.RequestIdProperty, requestId))
            using (LogContext.PushProperty(LoggingConstants.TraceIdProperty, traceId))
            {
                try
                {
                    await _next(context);

                    // Use ResultExtensions for successful request logging with performance tracking
                    var successResult = Result.Success();
                    successResult.OnSuccess(() =>
                        _logger.LogDebug("{RequestId} {Path} {Method} {StatusCode} - MiddlewareRequestCompleted",
                            requestId, context.Request.Path, context.Request.Method, context.Response.StatusCode));
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(context, ex, requestId);
                }
            }
        }
        #endregion

        #region Private Methods
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
                var startedResult = Result.BadRequest(ApiResponseMessages.Validation.ResponseAlreadyStarted);
                startedResult.OnFailure(errors =>
                    _logger.LogWarning(ApiResponseMessages.Logging.MiddlewareExceptionProcessingFailed, requestId, string.Join(", ", errors)));
                return;
            }

            // Set content type early for better performance
            context.Response.ContentType = "application/json; charset=utf-8";

            // Enhanced exception processing pipeline
            var exceptionProcessingResult = await ProcessExceptionAsync(context, exception, requestId);

            exceptionProcessingResult.OnSuccess(() =>
            _logger.LogDebug("{RequestId} - MiddlewareRequestCompleted", requestId))
            .OnFailure(errors =>
                _logger.LogError("{RequestId} - MiddlewareExceptionProcessingFailed: {Errors}", requestId, string.Join(", ", errors)));
        }

        private async Task<Result> ProcessExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            try
            {
                // Unwrap and categorize exception using functional approach
                var realException = GetInnermostException(exception);
                var categorization = CategorizeExceptionCore(realException);
                var (statusCode, message, logLevel) = categorization;

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
                var processingError = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                processingError.OnFailure(errors =>
                    _logger.LogCritical(ex, "{RequestId} - Critical error in exception processing", requestId));
                return processingError;
            }
        }

        private void LogException(Exception exception, HttpContext context, string requestId, string message, LogLevel logLevel)
        {
            var logResult = Result.Success();
            logResult.OnSuccess(() =>
            {
                // Use centralized middleware handled template
                var logArgs = new object[] { requestId, context.Request.Path, context.Request.Method, exception.GetType().Name, message };

                _ = logLevel switch
                {
                    LogLevel.Error => LogAndReturn(() => _logger.LogError(exception, ApiResponseMessages.Logging.MiddlewareExceptionHandled, logArgs)),
                    LogLevel.Warning => LogAndReturn(() => _logger.LogWarning(exception, ApiResponseMessages.Logging.MiddlewareExceptionHandled, logArgs)),
                    LogLevel.Information => LogAndReturn(() => _logger.LogInformation(exception, ApiResponseMessages.Logging.MiddlewareExceptionHandled, logArgs)),
                    LogLevel.Critical => LogAndReturn(() => _logger.LogCritical(exception, ApiResponseMessages.Logging.MiddlewareExceptionHandled, logArgs)),
                    _ => LogAndReturn(() => _logger.LogDebug(exception, ApiResponseMessages.Logging.MiddlewareExceptionHandled, logArgs))
                };
            });

            // Local helper function for cleaner pattern matching
            static object LogAndReturn(Action logAction)
            {
                logAction();
                return new object();
            }
        }
        #endregion

        #region Static Methods
        private static (int StatusCode, string Message, LogLevel LogLevel) CategorizeExceptionCore(Exception exception)
        {
            var ex = exception ?? new Exception("Unknown");
            return ex switch
            {
                DbUpdateConcurrencyException => ((int)HttpStatusCode.Conflict, ApiResponseMessages.Infrastructure.ConcurrencyConflict, LogLevel.Warning),
                DbUpdateException dbEx => ((int)HttpStatusCode.BadRequest, GetDatabaseErrorMessage(dbEx), LogLevel.Error),
                TimeoutException => ((int)HttpStatusCode.RequestTimeout, ApiResponseMessages.Infrastructure.RequestTimedOut, LogLevel.Warning),
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, ApiResponseMessages.Infrastructure.AccessDeniedAuthenticate, LogLevel.Warning),
                ArgumentNullException argEx => ((int)HttpStatusCode.BadRequest, string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, argEx.ParamName ?? "unknown"), LogLevel.Warning),
                ArgumentException argEx => ((int)HttpStatusCode.BadRequest, string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, argEx.ParamName ?? "unknown"), LogLevel.Warning),
                InvalidOperationException => ((int)HttpStatusCode.BadRequest, ApiResponseMessages.Infrastructure.InvalidOperation, LogLevel.Warning),
                JsonException => ((int)HttpStatusCode.BadRequest, ApiResponseMessages.Infrastructure.InvalidJsonFormat, LogLevel.Warning),
                OutOfMemoryException => ((int)HttpStatusCode.InternalServerError, ApiResponseMessages.Infrastructure.SystemHighLoad, LogLevel.Error),
                StackOverflowException => ((int)HttpStatusCode.InternalServerError, ApiResponseMessages.Infrastructure.SystemErrorContact, LogLevel.Error),
                HttpRequestException => ((int)HttpStatusCode.BadGateway, ApiResponseMessages.Infrastructure.ExternalServiceUnavailable, LogLevel.Warning),
                TaskCanceledException => ((int)HttpStatusCode.RequestTimeout, ApiResponseMessages.Infrastructure.RequestCancelled, LogLevel.Warning),
                OperationCanceledException => ((int)HttpStatusCode.RequestTimeout, ApiResponseMessages.Infrastructure.OperationCancelled, LogLevel.Warning),
                DirectoryNotFoundException => ((int)HttpStatusCode.InternalServerError, ApiResponseMessages.Infrastructure.RequiredResourceNotFound, LogLevel.Error),
                FileNotFoundException => ((int)HttpStatusCode.InternalServerError, ApiResponseMessages.Infrastructure.RequiredFileNotFound, LogLevel.Error),
                _ => ((int)HttpStatusCode.InternalServerError, ApiResponseMessages.Infrastructure.UnexpectedErrorDetailed, LogLevel.Error)
            };
        }

        private static string GetDatabaseErrorMessage(DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? string.Empty;
            ReadOnlySpan<char> messageSpan = innerMessage.AsSpan();

            if (messageSpan.Contains("UNIQUE constraint".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                messageSpan.Contains("duplicate key".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Infrastructure.DbUniqueConstraintViolation;
            }

            if (messageSpan.Contains("FOREIGN KEY constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Infrastructure.DbForeignKeyViolation;
            }

            if (messageSpan.Contains("CHECK constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Infrastructure.DbCheckConstraintViolation;
            }

            if (messageSpan.Contains("NOT NULL constraint".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseMessages.Infrastructure.DbNotNullViolation;
            }

            return ApiResponseMessages.Infrastructure.DbGenericError;
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
                var payload = JsonSerializer.Serialize(error, JsonOptions);
                await context.Response.WriteAsync(payload);

                var responseResult = Result.Success();
                responseResult.OnSuccess(() =>
                    // Use structured logging with matching placeholders to avoid FormatException
                    _logger.LogDebug("Error response written. RequestId={RequestId}, Code={Code}, PayloadLength={Length}", requestId, error.Code, payload.Length));

                return responseResult;
            }
            catch (Exception ex)
            {
                var errorResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                errorResult.OnFailure(errors =>
                    _logger.LogError(ex, ApiResponseMessages.Logging.MiddlewareCriticalProcessingError, requestId));
                return errorResult;
            }
        }

        private static Exception GetInnermostException(Exception ex)
        {
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
        #endregion
    }
    #endregion
}
