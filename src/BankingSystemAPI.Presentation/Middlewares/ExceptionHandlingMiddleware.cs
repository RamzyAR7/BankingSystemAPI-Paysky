using BankingSystemAPI.Application.Exceptions;
using FluentValidation;
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
                case ValidationException valEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case NotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    break;
                case BadRequestException:
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

            // If validation exception include errors collection
            if (realException is ValidationException vEx)
            {
                var failures = vEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                error.Details = failures;
            }

            var payload = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(payload);
        }

        private Exception GetInnermostException(Exception ex)
        {
            if (ex is AggregateException aex && aex.InnerExceptions != null && aex.InnerExceptions.Count > 0)
                return GetInnermostException(aex.InnerExceptions.First());

            return ex.InnerException ?? ex;
        }
    }
}
