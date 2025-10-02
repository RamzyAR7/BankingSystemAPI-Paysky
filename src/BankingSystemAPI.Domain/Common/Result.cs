using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Enhanced operation result without a value following best practices.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public bool IsFailure => !IsSuccess;
        public IReadOnlyList<string> Errors { get; private set; }
        public string ErrorMessage => string.Join("; ", Errors);

        // Keep old property for backward compatibility
        public bool Succeeded => IsSuccess;

        protected Result(bool isSuccess, IEnumerable<string> errors)
        {
            IsSuccess = isSuccess;
            Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }

        public static Result Success() => new Result(true, Enumerable.Empty<string>());
        
        public static Result Failure(params string[] errors) => new Result(false, errors);
        
        public static Result Failure(IEnumerable<string> errors) => new Result(false, errors);

        // Convenience methods for common scenarios
        public static Result NotFound(string entity, object id) => 
            Failure($"{entity} with ID '{id}' not found.");

        public static Result Unauthorized(string message = "Access denied.") => 
            Failure(message);

        public static Result Forbidden(string message = "Operation forbidden.") => 
            Failure(message);

        public static Result BadRequest(string message) => 
            Failure(message);

        // Implicit conversion for easier usage
        public static implicit operator bool(Result result) => result.IsSuccess;

        // Combine multiple results
        public static Result Combine(params Result[] results)
        {
            var failures = results.Where(r => r.IsFailure).ToList();
            if (!failures.Any()) return Success();
            
            var allErrors = failures.SelectMany(r => r.Errors);
            return Failure(allErrors);
        }
    }

    /// <summary>
    /// Enhanced operation result with a value following best practices.
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        private Result(bool isSuccess, T? value, IEnumerable<string> errors) 
            : base(isSuccess, errors)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => 
            new Result<T>(true, value, Enumerable.Empty<string>());
        
        public static new Result<T> Failure(params string[] errors) => 
            new Result<T>(false, default, errors);
        
        public static new Result<T> Failure(IEnumerable<string> errors) => 
            new Result<T>(false, default, errors);

        // Convenience methods for common scenarios
        public static new Result<T> NotFound(string entity, object id) => 
            Failure($"{entity} with ID '{id}' not found.");

        public static new Result<T> Unauthorized(string message = "Access denied.") => 
            Failure(message);

        public static new Result<T> Forbidden(string message = "Operation forbidden.") => 
            Failure(message);

        public static new Result<T> BadRequest(string message) => 
            Failure(message);

        // Transform result to different type
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (IsFailure) return Result<TNew>.Failure(Errors);
            return Result<TNew>.Success(mapper(Value!));
        }

        // Helper method to convert from base Result (explicit conversion)
        public static Result<T> FromBaseResult(Result result)
        {
            if (result.IsSuccess) 
                throw new InvalidOperationException("Cannot convert successful Result to Result<T> without a value.");
            return Failure(result.Errors);
        }
    }
}
