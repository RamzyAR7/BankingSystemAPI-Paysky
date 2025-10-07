#region Usings
using FluentValidation;
using MediatR;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using BankingSystemAPI.Domain.Constant;
using FluentValidation.Results;
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
                var noValidatorsResult = Result.Success();
                noValidatorsResult.OnSuccess(() => 
                    _logger.LogDebug(ApiResponseMessages.Logging.ValidationPipelineNoValidators, typeof(TRequest).Name));
                
                return await next();
            }

            var validationResult = await ExecuteValidationAsync(request, cancellationToken);
            
            if (validationResult.IsFailure)
            {
                return CreateValidationFailureResponse(validationResult.ErrorItems, typeof(TRequest).Name);
            }

            // Log successful validation using ResultExtensions
            validationResult.OnSuccess(() => 
                _logger.LogDebug(ApiResponseMessages.Logging.ValidationPipelinePassed, 
                    typeof(TRequest).Name, _validators.Count()));

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

                // Convert FluentValidation failures to structured ResultError items
                var structuredErrors = failures
                    .Select(f => new ResultError(
                        ErrorType.Validation,
                        f.ErrorMessage ?? "Validation failed",
                        new ResultErrorDetails(
                            Field: f.PropertyName,
                            Code: f.ErrorCode,
                            Metadata: f.AttemptedValue
                        )
                    ))
                    .ToList();

                var validationFailureResult = Result.Failure(structuredErrors);
                
                // Use ResultExtensions for structured validation failure logging (OnFailure supplies string list)
                validationFailureResult.OnFailure(errs => 
                    _logger.LogWarning(ApiResponseMessages.Logging.ValidationPipelineFailed, 
                        typeof(TRequest).Name, string.Join(", ", errs), _validators.Count()));

                return validationFailureResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, ApiResponseMessages.Logging.ValidationPipelineException, typeof(TRequest).Name));
                
                return exceptionResult;
            }
        }

        private TResponse CreateValidationFailureResponse(IReadOnlyList<ResultError> errors, string requestTypeName)
        {
            // If the pipeline response is a Result (non-generic)
            if (typeof(TResponse) == typeof(Result))
            {
                var failure = Result.Failure(errors);
                // Use ResultExtensions for logging
                failure.OnFailure(errs => 
                    _logger.LogInformation(ApiResponseMessages.Logging.ValidationPipelineReturningFailure, "Result", requestTypeName));
                return (TResponse)(object)failure;
            }

            // If the pipeline response is Result<T>, construct a failed Result<T> dynamically - reflection
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericArg = responseType.GetGenericArguments()[0];
                var resultGenericType = typeof(Result<>).MakeGenericType(genericArg);
                var failureMethod = resultGenericType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IEnumerable<ResultError>) }, null);
                
                if (failureMethod != null)
                {
                    var failureInstance = failureMethod.Invoke(null, new object[] { errors });
                    
                    // Use ResultExtensions patterns for logging generic result failures
                    var logResult = Result.Success();
                    logResult.OnSuccess(() => 
                        _logger.LogInformation(ApiResponseMessages.Logging.ValidationPipelineReturningFailure, 
                            genericArg.Name, requestTypeName));
                        
                    return (TResponse)failureInstance!;
                }
            }

            // Fallback to throwing exception for non-Result types with enhanced logging
            var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, "Unsupported response type for validation failure"));
            exceptionResult.OnFailure(errs => 
                _logger.LogError(ApiResponseMessages.Logging.ValidationPipelineUnsupportedResponse, 
                    typeof(TResponse).Name, requestTypeName));

            throw new ValidationException(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, $"Validation failed for {requestTypeName}: {string.Join(", ", errors.Select(e=>e.Message))}"));
        }
    }
}

