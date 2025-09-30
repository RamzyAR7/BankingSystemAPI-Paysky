using FluentValidation;
using MediatR;
using BankingSystemAPI.Application.Common;
using System.Reflection;

namespace BankingSystemAPI.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = _validators
                    .Select(v => v.Validate(context))
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (validationResults.Any())
                {
                    // Prepare distinct error messages
                    var errors = validationResults
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct()
                        .ToArray();

                    // If the pipeline response is a Result (non-generic)
                    if (typeof(TResponse) == typeof(Result))
                    {
                        var failure = Result.Failure(errors);
                        return (TResponse)(object)failure!;
                    }

                    // If the pipeline response is Result<T>, construct a failed Result<T> dynamically
                    var responseType = typeof(TResponse);
                    if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var genericArg = responseType.GetGenericArguments()[0];
                        var resultGenericType = typeof(Result<>).MakeGenericType(genericArg);
                        var failureMethod = resultGenericType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static);
                        var failureInstance = failureMethod.Invoke(null, new object[] { errors });
                        return (TResponse)failureInstance!;
                    }

                    throw new ValidationException(validationResults);
                }
            }

            return await next();
        }
    }
}
