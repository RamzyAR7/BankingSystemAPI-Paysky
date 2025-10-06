using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Presentation.Services
{
    public class ErrorResponseFactory : IErrorResponseFactory
    {
        private readonly ILogger<ErrorResponseFactory> _logger;

        public ErrorResponseFactory(ILogger<ErrorResponseFactory> logger)
        {
            _logger = logger;
        }

        public (int StatusCode, object Body) Create(IReadOnlyList<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                var bodyEmpty = new { success = false, message = ApiResponseMessages.Generic.UnknownError };
                return (400, bodyEmpty);
            }

            var first = errors[0] ?? string.Empty;
            var code = GetStatusCodeFromSemanticError(first);
            var body = new { success = false, errors = errors, message = string.Join("; ", errors) };

            return (code, body);
        }

        private static int GetStatusCodeFromSemanticError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return 400;

            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.NotAuthenticated, StringComparison.OrdinalIgnoreCase)) return 401;
            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AccessDenied, StringComparison.OrdinalIgnoreCase)) return 403;
            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.NotFound, StringComparison.OrdinalIgnoreCase) || errorMessage.Contains(ApiResponseMessages.ErrorPatterns.DoesNotExist, StringComparison.OrdinalIgnoreCase)) return 404;
            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AlreadyExists, StringComparison.OrdinalIgnoreCase) || errorMessage.Contains(ApiResponseMessages.ErrorPatterns.InsufficientFunds, StringComparison.OrdinalIgnoreCase) || errorMessage.Contains(ApiResponseMessages.ErrorPatterns.AccountInactive, StringComparison.OrdinalIgnoreCase)) return 409;
            if (errorMessage.Contains(ApiResponseMessages.ErrorPatterns.ValidationFailed, StringComparison.OrdinalIgnoreCase) || errorMessage.Contains(ApiResponseMessages.ErrorPatterns.BusinessRule, StringComparison.OrdinalIgnoreCase)) return 422;

            var lower = errorMessage.ToLowerInvariant();
            if (new[] { "not found", "does not exist", "no longer exists" }.Any(x => lower.Contains(x))) return 404;
            if (new[] { "not authenticated", "invalid credentials", "token expired", "invalid token" }.Any(x => lower.Contains(x))) return 401;
            if (new[] { "access denied", "insufficient permissions", "not authorized", "forbidden" }.Any(x => lower.Contains(x)) || (lower.Contains("permission") && lower.Contains("denied"))) return 403;
            if (new[] { "already exists", "duplicate", "conflict", "insufficient funds", "account is inactive", "account is locked", "account is closed", "daily limit exceeded", "transaction limit" }.Any(x => lower.Contains(x)) || (lower.Contains("balance") && lower.Contains("insufficient"))) return 409;
            if (new[] { "invalid amount", "validation failed", "business rule", "constraint violation", "amount must be positive", "invalid transaction type" }.Any(x => lower.Contains(x))) return 422;

            return 400;
        }
    }
}
