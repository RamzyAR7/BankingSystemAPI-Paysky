#region Usings
using FluentValidation;
using MediatR;
using BankingSystemAPI.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Reflection;
using BankingSystemAPI.Domain.Constant;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                _logger.LogDebug(ApiResponseMessages.Logging.ValidationPipelineNoValidators, typeof(TRequest).Name);
                return await next();
            }

            var validationResult = await ExecuteValidationAsync(request, cancellationToken);
            if (validationResult.IsFailure)
                return CreateValidationFailureResponse(validationResult.ErrorItems, typeof(TRequest).Name);

            _logger.LogDebug(ApiResponseMessages.Logging.ValidationPipelinePassed, typeof(TRequest).Name, _validators.Count());
            return await next();
        }

        private async Task<Result> ExecuteValidationAsync(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (!failures.Any())
                    return Result.Success();

                // Convert FluentValidation failures to structured ResultError items and return failure
                var structuredErrors = failures
                    .Select(f => new ResultError(
                        ErrorType.Validation,
                        f.ErrorMessage ?? "Validation failed",
                        new ResultErrorDetails(f.PropertyName, f.ErrorCode, f.AttemptedValue)))
                    .ToList();

                _logger.LogWarning(ApiResponseMessages.Logging.ValidationPipelineFailed,
                    typeof(TRequest).Name, string.Join(", ", structuredErrors.Select(e => e.Message)), _validators.Count());

                return Result.Failure(structuredErrors);
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                _logger.LogError(ex, ApiResponseMessages.Logging.ValidationPipelineException, typeof(TRequest).Name);
                return exceptionResult;
            }
        }

        private TResponse CreateValidationFailureResponse(IReadOnlyList<ResultError> errors, string requestTypeName)
        {
            // Non-generic Result
            if (typeof(TResponse) == typeof(Result))
            {
                _logger.LogInformation(ApiResponseMessages.Logging.ValidationPipelineReturningFailure, "Result", requestTypeName);
                return (TResponse)(object)Result.Failure(errors);
            }

            // Result<T> handling via small helper
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericArg = responseType.GetGenericArguments()[0];
                var failureInstance = CreateGenericFailure(genericArg, errors);
                if (failureInstance != null)
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.ValidationPipelineReturningFailure, genericArg.Name, requestTypeName);
                    return (TResponse)failureInstance;
                }
            }

            _logger.LogError(ApiResponseMessages.Logging.ValidationPipelineUnsupportedResponse, typeof(TResponse).Name, requestTypeName);
            throw new ValidationException(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, $"Validation failed for {requestTypeName}: {string.Join(", ", errors.Select(e => e.Message))}"));
        }

        private static object? CreateGenericFailure(Type genericArg, IEnumerable<ResultError> errors)
        {
            var resultGenericType = typeof(Result<>).MakeGenericType(genericArg);
            var failureMethod = resultGenericType.GetMethod("Failure", new[] { typeof(IEnumerable<ResultError>) });
            return failureMethod?.Invoke(null, new object[] { errors });
        }
    }
}
