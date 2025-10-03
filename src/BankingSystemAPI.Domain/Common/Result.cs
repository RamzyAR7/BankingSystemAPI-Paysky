using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Semantic factory methods for different HTTP status scenarios

        /// <summary>
        /// Creates a Not Found result (404) - Resource doesn't exist
        /// </summary>
        public static Result NotFound(string entity, object id) => 
            Failure($"{entity} with ID '{id}' not found.");

        public static Result NotFound(string message) => 
            Failure(message.Contains("not found") ? message : $"{message} not found.");

        /// <summary>
        /// Creates an Unauthorized result (401) - Authentication required
        /// </summary>
        public static Result Unauthorized(string message = "Not authenticated. Please login to access this resource.") => 
            Failure(message);

        /// <summary>
        /// Creates a Forbidden result (403) - Insufficient permissions
        /// </summary>
        public static Result Forbidden(string message = "Access denied. Insufficient permissions for this operation.") => 
            Failure(message);

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
            Failure($"Insufficient funds. Requested: {requested:C}, Available: {available:C}.");

        /// <summary>
        /// Creates a result for inactive account scenario
        /// </summary>
        public static Result AccountInactive(string accountNumber) => 
            Failure($"Account {accountNumber} is inactive and cannot perform transactions.");

        /// <summary>
        /// Creates a result for duplicate resource scenario
        /// </summary>
        public static Result AlreadyExists(string entity, string identifier) => 
            Failure($"{entity} '{identifier}' already exists.");

        /// <summary>
        /// Creates a result for invalid credentials scenario
        /// </summary>
        public static Result InvalidCredentials(string message = "Email or password is incorrect.") => 
            Failure(message);

        /// <summary>
        /// Creates a result for expired token scenario
        /// </summary>
        public static Result TokenExpired(string message = "Token has expired. Please login again.") => 
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
            Failure($"{entity} with ID '{id}' not found.");

        public static new Result<T> NotFound(string message) => 
            Failure(message.Contains("not found") ? message : $"{message} not found.");

        public static new Result<T> Unauthorized(string message = "Not authenticated. Please login to access this resource.") => 
            Failure(message);

        public static new Result<T> Forbidden(string message = "Access denied. Insufficient permissions for this operation.") => 
            Failure(message);

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
            Failure($"Insufficient funds. Requested: {requested:C}, Available: {available:C}.");

        public static new Result<T> AccountInactive(string accountNumber) => 
            Failure($"Account {accountNumber} is inactive and cannot perform transactions.");

        public static new Result<T> AlreadyExists(string entity, string identifier) => 
            Failure($"{entity} '{identifier}' already exists.");

        public static new Result<T> InvalidCredentials(string message = "Email or password is incorrect.") => 
            Failure(message);

        public static new Result<T> TokenExpired(string message = "Token has expired. Please login again.") => 
            Failure(message);

        // Implicit conversion from value
        public static implicit operator Result<T>(T value) => Success(value);
    }
}
