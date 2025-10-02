using BankingSystemAPI.Domain.Common;

namespace BankingSystemAPI.Application.Extensions
{
    /// <summary>
    /// Extension methods for Result pattern to improve usability
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Execute action if result is successful
        /// </summary>
        public static Result OnSuccess(this Result result, Action action)
        {
            if (result.IsSuccess)
                action();
            return result;
        }

        /// <summary>
        /// Execute action if result is successful (async)
        /// </summary>
        public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> action)
        {
            if (result.IsSuccess)
                await action();
            return result;
        }

        /// <summary>
        /// Execute action if result fails
        /// </summary>
        public static Result OnFailure(this Result result, Action<IReadOnlyList<string>> action)
        {
            if (result.IsFailure)
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Transform Result<T> to Result<TNew>
        /// </summary>
        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Result<T> result, Func<T, Task<TNew>> mapper)
        {
            if (result.IsFailure) 
                return Result<TNew>.Failure(result.Errors);
            
            var newValue = await mapper(result.Value!);
            return Result<TNew>.Success(newValue);
        }

        /// <summary>
        /// Bind operation - chain Results together
        /// </summary>
        public static Result<TNew> Bind<T, TNew>(this Result<T> result, Func<T, Result<TNew>> func)
        {
            if (result.IsFailure)
                return Result<TNew>.Failure(result.Errors);
            
            return func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Results together (async)
        /// </summary>
        public static async Task<Result<TNew>> BindAsync<T, TNew>(this Result<T> result, Func<T, Task<Result<TNew>>> func)
        {
            if (result.IsFailure)
                return Result<TNew>.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Convert Task<Result<T>> to Result<T> for synchronous contexts
        /// </summary>
        public static Result<T> GetAwaiter<T>(this Task<Result<T>> task)
        {
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validate multiple results and return combined result
        /// </summary>
        public static Result ValidateAll(params Result[] results)
        {
            return Result.Combine(results);
        }

        /// <summary>
        /// Convert nullable value to Result
        /// </summary>
        public static Result<T> ToResult<T>(this T? value, string errorMessage) where T : class
        {
            return value == null ? Result<T>.Failure(errorMessage) : Result<T>.Success(value);
        }

        /// <summary>
        /// Convert nullable value to Result (value types)
        /// </summary>
        public static Result<T> ToResult<T>(this T? value, string errorMessage) where T : struct
        {
            return value.HasValue ? Result<T>.Success(value.Value) : Result<T>.Failure(errorMessage);
        }
    }
}