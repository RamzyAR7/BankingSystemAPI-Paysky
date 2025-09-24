using BankingSystemAPI.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankingSystemAPI.Presentation.Middlewares
{
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

            // Unwrap AggregateException / inner exceptions so wrapped custom exceptions are handled properly
            var realException = GetInnermostException(exception);

            int statusCode;

            switch (realException)
            {
                case AccountNotFoundException:
                case TransactionNotFoundException:
                case CurrencyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    break;
                case NotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    break;
                case BadRequestException:
                case InvalidAccountOperationException:
                case ArgumentNullException:
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedException:
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case ForbiddenException:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    break;
                case DbUpdateConcurrencyException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    break;
                case DbUpdateException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            context.Response.StatusCode = statusCode;

            var error = new ErrorDetails
            {
                Code = statusCode.ToString(),
                Message = realException.Message,
                RequestId = requestId
            };

            // Log exception only
            _logger.LogError(exception, "Error in Request {RequestId}: {Message}", requestId, exception.Message);

            var result = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(result);
        }

        private Exception GetInnermostException(Exception ex)
        {
            if (ex == null) return ex;

            // Unwrap AggregateException which may wrap exceptions thrown from tasks
            if (ex is AggregateException agg && agg.InnerException != null)
            {
                ex = agg.Flatten().InnerException ?? ex;
            }

            while (ex.InnerException != null)
                ex = ex.InnerException;

            return ex;
        }

        private class ErrorDetails
        {
            public string Code { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string RequestId { get; set; } = string.Empty;
        }
    }
}
