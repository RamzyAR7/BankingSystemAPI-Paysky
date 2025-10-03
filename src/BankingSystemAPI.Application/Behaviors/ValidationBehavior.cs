using FluentValidation;
using MediatR;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
                // Use ResultExtensions patterns for consistent logging
                var noValidatorsResult = Result.Success();
                noValidatorsResult.OnSuccess(() => 
                    _logger.LogDebug("[VALIDATION_PIPELINE] No validators found for request type: {RequestType}", typeof(TRequest).Name));
                
                return await next();
            }

            var validationResult = await ExecuteValidationAsync(request, cancellationToken);
            
            if (validationResult.IsFailure)
            {
                return CreateValidationFailureResponse(validationResult.Errors, typeof(TRequest).Name);
            }

            // Log successful validation using ResultExtensions
            validationResult.OnSuccess(() => 
                _logger.LogDebug("[VALIDATION_PIPELINE] Validation passed for request type: {RequestType}, Validators: {ValidatorCount}", 
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

                // Prepare distinct error messages
                var errors = failures
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct()
                    .ToArray();

                var validationFailureResult = Result.Failure(errors);
                
                // Use ResultExtensions for structured validation failure logging
                validationFailureResult.OnFailure(errs => 
                    _logger.LogWarning("[VALIDATION_PIPELINE] Validation failed for request type: {RequestType}, Errors: {Errors}, ValidatorCount: {ValidatorCount}", 
                        typeof(TRequest).Name, string.Join(", ", errs), _validators.Count()));

                return validationFailureResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest($"Validation pipeline error: {ex.Message}");
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, "[VALIDATION_PIPELINE] Exception during validation for request type: {RequestType}", typeof(TRequest).Name));
                
                return exceptionResult;
            }
        }

        private TResponse CreateValidationFailureResponse(IReadOnlyList<string> errors, string requestTypeName)
        {
            // If the pipeline response is a Result (non-generic)
            if (typeof(TResponse) == typeof(Result))
            {
                var failure = Result.Failure(errors);
                
                // Use ResultExtensions for logging
                failure.OnFailure(errs => 
                    _logger.LogInformation("[VALIDATION_PIPELINE] Returning validation failure result for: {RequestType}", requestTypeName));
                
                return (TResponse)(object)failure;
            }

            // If the pipeline response is Result<T>, construct a failed Result<T> dynamically
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericArg = responseType.GetGenericArguments()[0];
                var resultGenericType = typeof(Result<>).MakeGenericType(genericArg);
                var failureMethod = resultGenericType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IEnumerable<string>) }, null);
                
                if (failureMethod != null)
                {
                    var failureInstance = failureMethod.Invoke(null, new object[] { errors });
                    
                    // Use ResultExtensions patterns for logging generic result failures
                    var logResult = Result.Success();
                    logResult.OnSuccess(() => 
                        _logger.LogInformation("[VALIDATION_PIPELINE] Returning validation failure Result<{GenericType}> for: {RequestType}", 
                            genericArg.Name, requestTypeName));
                    
                    return (TResponse)failureInstance!;
                }
            }

            // Fallback to throwing exception for non-Result types with enhanced logging
            var exceptionResult = Result.BadRequest("Unsupported response type for validation failure");
            exceptionResult.OnFailure(errs => 
                _logger.LogError("[VALIDATION_PIPELINE] Unsupported response type {ResponseType} for validation failure in request: {RequestType}", 
                    typeof(TResponse).Name, requestTypeName));

            throw new ValidationException($"Validation failed for {requestTypeName}: {string.Join(", ", errors)}");
        }
    }
}
