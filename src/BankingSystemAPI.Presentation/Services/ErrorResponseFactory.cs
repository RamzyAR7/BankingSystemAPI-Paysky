using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Common;

namespace BankingSystemAPI.Presentation.Services
{
    public class ErrorResponseFactory : IErrorResponseFactory
    {
        private readonly ILogger<ErrorResponseFactory> _logger;

        public ErrorResponseFactory(ILogger<ErrorResponseFactory> logger)
        {
            _logger = logger;
        }

        public (int StatusCode, object Body) Create(IReadOnlyList<ResultError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                var bodyEmpty = new { success = false, message = "Unknown error occurred." };
                return (400, bodyEmpty);
            }

            var primary = errors[0];
            var code = ResultErrorMapper.MapToStatusCode(primary.Type);
            var body = new
            {
                success = false,
                errors = errors.Select(e => new { type = e.Type.ToString(), message = e.Message, details = e.Details }).ToArray(),
                message = string.Join("; ", errors.Select(e => e.Message))
            };

            return (code, body);
        }
    }
}
