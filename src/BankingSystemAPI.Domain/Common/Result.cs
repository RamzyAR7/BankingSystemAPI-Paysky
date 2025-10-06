#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Enhanced operation result without a value following best practices.
    /// Now includes semantic factory methods for proper HTTP status code mapping.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public bool IsFailure => !IsSuccess;
        public IReadOnlyList<string> Errors { get; private set; }
        public string ErrorMessage => string.Join("; ", Errors);

        protected Result(bool isSuccess, IEnumerable<string> errors)
        {
            IsSuccess = isSuccess;
            Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }

        public static Result Success() => new Result(true, Enumerable.Empty<string>());
        
        public static Result Failure(params string[] errors) => new Result(false, errors);
        
        public static Result Failure(IEnumerable<string> errors) => new Result(false, errors);

        // Semantic factory methods for different HTTP status scenarios

        /// <summary>
        /// Creates a Not Found result (404) - Resource doesn't exist
        /// </summary>
        public static Result NotFound(string entity, object id) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, entity, id));

        public static Result NotFound(string message) => 
            Failure(message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? message : string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, message, ""));

        /// <summary>
        /// Creates an Unauthorized result (401) - Authentication required
        /// </summary>
        public static Result Unauthorized(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.NotAuthenticated);

        /// <summary>
        /// Creates a Forbidden result (403) - Insufficient permissions
        /// </summary>
        public static Result Forbidden(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.AccessDenied);

        /// <summary>
        /// Creates a Bad Request result (400) - Invalid input/request format
        /// </summary>
        public static Result BadRequest(string message) => 
            Failure(message);

        /// <summary>
        /// Creates a Conflict result (409) - Business rule violation or resource conflict
        /// </summary>
        public static Result Conflict(string message) => 
            Failure(message);

        /// <summary>
        /// Creates an Unprocessable Entity result (422) - Valid format but business validation failed
        /// </summary>
        public static Result ValidationFailed(string message) => 
            Failure(message);

        public static Result ValidationFailed(params string[] validationErrors) => 
            Failure(validationErrors);

        // Common business scenarios for banking system

        /// <summary>
        /// Creates a result for insufficient funds scenario
        /// </summary>
        public static Result InsufficientFunds(decimal requested, decimal available) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.InsufficientFundsFormat, requested.ToString("C"), available.ToString("C")));

        /// <summary>
        /// Creates a result for inactive account scenario
        /// </summary>
        public static Result AccountInactive(string accountNumber) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.AccountInactiveFormat, accountNumber));

        /// <summary>
        /// Creates a result for duplicate resource scenario
        /// </summary>
        public static Result AlreadyExists(string entity, string identifier) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, entity, identifier));

        /// <summary>
        /// Creates a result for invalid credentials scenario
        /// </summary>
        public static Result InvalidCredentials(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.InvalidCredentials);

        /// <summary>
        /// Creates a result for expired token scenario
        /// </summary>
        public static Result TokenExpired(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.TokenExpired);

        // Implicit conversion for easier usage
        public static implicit operator bool(Result result) => result.IsSuccess;

        // Combine multiple results
        public static Result Combine(params Result[] results)
        {
            var failures = results.Where(r => !r).ToList(); // Using implicit bool operator!
            if (!failures.Any()) return Success();
            
            var allErrors = failures.SelectMany(r => r.Errors);
            return Failure(allErrors);
        }
    }

    /// <summary>
    /// Enhanced operation result with a value following best practices.
    /// Includes semantic factory methods for proper HTTP status code mapping.
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

        // Semantic factory methods for different HTTP status scenarios

        public static new Result<T> NotFound(string entity, object id) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, entity, id));

        public static new Result<T> NotFound(string message) => 
            Failure(message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? message : string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, message, ""));

        public static new Result<T> Unauthorized(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.NotAuthenticated);

        public static new Result<T> Forbidden(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.AccessDenied);

        public static new Result<T> BadRequest(string message) => 
            Failure(message);

        public static new Result<T> Conflict(string message) => 
            Failure(message);

        public static new Result<T> ValidationFailed(string message) => 
            Failure(message);

        public static new Result<T> ValidationFailed(params string[] validationErrors) => 
            Failure(validationErrors);

        // Common business scenarios for banking system

        public static new Result<T> InsufficientFunds(decimal requested, decimal available) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.InsufficientFundsFormat, requested.ToString("C"), available.ToString("C")));

        public static new Result<T> AccountInactive(string accountNumber) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.AccountInactiveFormat, accountNumber));

        public static new Result<T> AlreadyExists(string entity, string identifier) => 
            Failure(string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, entity, identifier));

        public static new Result<T> InvalidCredentials(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.InvalidCredentials);

        public static new Result<T> TokenExpired(string message = null) => 
            Failure(message ?? ApiResponseMessages.ErrorPatterns.TokenExpired);

        // Implicit conversion from value
        public static implicit operator Result<T>(T value) => Success(value);
    }
}
