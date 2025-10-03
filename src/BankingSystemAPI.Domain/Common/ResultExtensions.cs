using BankingSystemAPI.Domain.Common;

namespace BankingSystemAPI.Domain.Extensions
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
        /// Execute action if Task<Result<T>> is successful
        /// </summary>
        public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Action action)
        {
            var result = await resultTask;
            if (result.IsSuccess)
                action();
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result<T>> fails
        /// </summary>
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask;
            if (result.IsFailure)
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result> is successful
        /// </summary>
        public static async Task<Result> OnSuccess(this Task<Result> resultTask, Action action)
        {
            var result = await resultTask;
            if (result.IsSuccess)
                action();
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result> fails
        /// </summary>
        public static async Task<Result> OnFailure(this Task<Result> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask;
            if (result.IsFailure)
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Transform Result<T> to Result<TNew> synchronously
        /// </summary>
        public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
        {
            if (result.IsFailure) 
                return Result<TNew>.Failure(result.Errors);
            
            var newValue = mapper(result.Value!);
            return Result<TNew>.Success(newValue);
        }

        /// <summary>
        /// Transform Result<T> to Result<TNew> asynchronously
        /// </summary>
        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Result<T> result, Func<T, Task<TNew>> mapper)
        {
            if (result.IsFailure) 
                return Result<TNew>.Failure(result.Errors);
            
            var newValue = await mapper(result.Value!);
            return Result<TNew>.Success(newValue);
        }

        /// <summary>
        /// Transform Task<Result<T>> to Task<Result<TNew>>
        /// </summary>
        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Task<Result<T>> resultTask, Func<T, Task<TNew>> mapper)
        {
            var result = await resultTask;
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
        /// Bind operation - chain Result<T> to Result
        /// </summary>
        public static Result Bind<T>(this Result<T> result, Func<T, Result> func)
        {
            if (result.IsFailure)
                return Result.Failure(result.Errors);
            
            return func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result to Result<T>
        /// </summary>
        public static Result<T> Bind<T>(this Result result, Func<Result<T>> func)
        {
            if (result.IsFailure)
                return Result<T>.Failure(result.Errors);
            
            return func();
        }

        /// <summary>
        /// Bind operation - chain Result to Result
        /// </summary>
        public static Result Bind(this Result result, Func<Result> func)
        {
            if (result.IsFailure)
                return result;
            
            return func();
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
        /// Bind operation - chain Result<T> to Result (async)
        /// </summary>
        public static async Task<Result> BindAsync<T>(this Result<T> result, Func<T, Task<Result>> func)
        {
            if (result.IsFailure)
                return Result.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result to Task<Result<T>>
        /// </summary>
        public static async Task<Result<T>> BindAsync<T>(this Result result, Func<Task<Result<T>>> func)
        {
            if (result.IsFailure)
                return Result<T>.Failure(result.Errors);
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Result to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync(this Result result, Func<Task<Result>> func)
        {
            if (result.IsFailure)
                return result;
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Task<Result<T>> to Task<Result<TNew>>
        /// </summary>
        public static async Task<Result<TNew>> BindAsync<T, TNew>(this Task<Result<T>> resultTask, Func<T, Task<Result<TNew>>> func)
        {
            var result = await resultTask;
            if (result.IsFailure)
                return Result<TNew>.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Task<Result<T>> to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<Result>> func)
        {
            var result = await resultTask;
            if (result.IsFailure)
                return Result.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Task<Result> to Task<Result<T>>
        /// </summary>
        public static async Task<Result<T>> BindAsync<T>(this Task<Result> resultTask, Func<Task<Result<T>>> func)
        {
            var result = await resultTask;
            if (result.IsFailure)
                return Result<T>.Failure(result.Errors);
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Task<Result> to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync(this Task<Result> resultTask, Func<Task<Result>> func)
        {
            var result = await resultTask;
            if (result.IsFailure)
                return result;
            
            return await func();
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