#region Usings
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BankingSystemAPI.Domain.Constant;
#endregion

namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Structured metadata for an error (optional)
    /// </summary>
    public sealed record ResultErrorDetails(string? Field = null, string? Code = null, object? Metadata = null);

    /// <summary>
    /// Single error item used by Result. Immutable and strongly typed.
    /// </summary>
    public sealed record ResultError(ErrorType Type, string Message, ResultErrorDetails? Details = null);

    /// <summary>
    /// Enhanced operation result without a value. Immutable (record class).
    /// Preserves backward-compatible string Errors list while exposing structured errors.
    /// </summary>
    public record class Result
    {
        // Internal list of structured errors
        private readonly IReadOnlyList<ResultError> _errorItems;
        private readonly string _errorMessageCache; // cache joined message to avoid repeated joins

        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;

        // Structured errors (new API)
        public IReadOnlyList<ResultError> ErrorItems => _errorItems;

        // Backwards-compatible view: plain strings
        public IReadOnlyList<string> Errors { get; init; }

        // Cached joined message (same semantics as previous code)
        public string ErrorMessage => _errorMessageCache;

        // Primary error type derived from first error or Unknown
        public ErrorType PrimaryErrorType => _errorItems.Count > 0 ? _errorItems[0].Type : ErrorType.Unknown;

        // Resolve HTTP status code using centralized mapper
        public int StatusCode => ResultErrorMapper.MapToStatusCode(PrimaryErrorType);

        // Protected ctor used by factory methods
        protected Result(bool isSuccess, IReadOnlyList<ResultError> errorItems)
        {
            IsSuccess = isSuccess;
            _errorItems = errorItems ?? Array.Empty<ResultError>();
            Errors = new ReadOnlyCollection<string>(_errorItems.Select(e => e.Message).ToList());
            _errorMessageCache = string.Join("; ", Errors);
        }

        public static Result Success() => new Result(true, Array.Empty<ResultError>());

        // Removed string[] overload for Failure to enforce structured error usage
        public static Result Failure(ErrorType type, string message, ResultErrorDetails? details = null) =>
            Failure(new ResultError(type, message, details));
        public static Result Failure(params ResultError[] errors) =>
            new Result(false, errors?.ToList().AsReadOnly() ?? new List<ResultError>().AsReadOnly());
        public static Result Failure(IEnumerable<ResultError> errors) =>
            new Result(false, errors?.ToList().AsReadOnly() ?? new List<ResultError>().AsReadOnly());
        public static Result Failure(IEnumerable<string>? messages) =>
            Failure(messages?.Select(m => new ResultError(ErrorType.Validation, m)) ?? Array.Empty<ResultError>());
        public static Result NotFound(string entity, object id) =>
            Failure(ErrorType.NotFound, string.Format("{0} with id {1} was not found.", entity, id));

        public static Result Unauthorized(string message = null) =>
            Failure(ErrorType.Unauthorized, message ?? "Not authenticated.");

        public static Result Forbidden(string message = null) =>
            Failure(ErrorType.Forbidden, message ?? "Access denied.");

        public static Result BadRequest(string message) =>
            Failure(ErrorType.Validation, message);

        public static Result Conflict(string message) =>
            Failure(ErrorType.Conflict, message);

        public static Result ValidationFailed(string message) =>
            Failure(ErrorType.Validation, message);

        public static Result InsufficientFunds(decimal requested, decimal available) =>
            Failure(ErrorType.BusinessRule, string.Format("Insufficient funds: requested {0}, available {1}.", requested.ToString("C"), available.ToString("C")));

        public static Result AccountInactive(string accountNumber) =>
            Failure(ErrorType.BusinessRule, string.Format("Account {0} is inactive.", accountNumber));

        public static Result Combine(params Result[] results)
        {
            // Removed IEnumerable<string> overload for Failure to enforce structured error usage
            var failures = results.Where(r => r.IsFailure).ToList();
            if (!failures.Any()) return Success();

            var combinedErrors = failures.SelectMany(r => r.ErrorItems).ToList();
            return Failure(combinedErrors);
        }

        public static implicit operator bool(Result result) => result is not null && result.IsSuccess;
    }

    /// <summary>
    /// Generic Result with value. Immutable (record class) and backward-compatible.
    /// </summary>
    public record class Result<T> : Result
    {
        public T? Value { get; init; }

        private Result(bool isSuccess, T? value, IReadOnlyList<ResultError> errors)
            : base(isSuccess, errors)
        {
            Value = value;
        }

        // Success / Failure factories
        public static Result<T> Success(T value) => new Result<T>(true, value, Array.Empty<ResultError>());

        #region Failure Managers

        public static Result<T> Failure(params ResultError[] errors) =>
            new Result<T>(false, default, errors?.ToList().AsReadOnly() ?? new List<ResultError>().AsReadOnly());

        public static Result<T> Failure(IEnumerable<ResultError> errors) =>
            new Result<T>(false, default, errors?.ToList().AsReadOnly() ?? new List<ResultError>().AsReadOnly());


        public static Result<T> Failure(ErrorType type, string message, ResultErrorDetails? details = null) =>
            Failure(new ResultError(type, message, details));
        #endregion
        // Semantic factories
        public static new Result<T> NotFound(string entity, object id) =>
            Failure(ErrorType.NotFound, string.Format("{0} with id {1} was not found.", entity, id));

        public static new Result<T> NotFound(string message) =>
            Failure(ErrorType.NotFound, message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? message
                : string.Format("{0} not found.", message));

        public static new Result<T> Unauthorized(string message = null) =>
            Failure(ErrorType.Unauthorized, message ?? "Not authenticated.");

        public static new Result<T> Forbidden(string message = null) =>
            Failure(ErrorType.Forbidden, message ?? "Access denied.");

        public static new Result<T> BadRequest(string message) =>
            Failure(ErrorType.Validation, message);

        public static new Result<T> Conflict(string message) =>
            Failure(ErrorType.Conflict, message);

        public static new Result<T> ValidationFailed(string message) =>
            Failure(ErrorType.Validation, message);

        public static new Result<T> InsufficientFunds(decimal requested, decimal available) =>
            Failure(ErrorType.BusinessRule, string.Format("Insufficient funds: requested {0}, available {1}.", requested.ToString("C"), available.ToString("C")));

        public static new Result<T> InvalidCredentials(string message = null) =>
            Failure(ErrorType.Unauthorized, message ?? "Email or password is incorrect.");

        // Implicit conversion from value
        public static implicit operator Result<T>(T value) => Success(value);
    }
}
