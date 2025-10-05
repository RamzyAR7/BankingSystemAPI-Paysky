#region Usings
using BankingSystemAPI.Domain.Common;
#endregion


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
            if (result) // Using implicit bool operator!
                action();
            return result;
        }

        /// <summary>
        /// Execute action if result fails
        /// </summary>
        public static Result OnFailure(this Result result, Action<IReadOnlyList<string>> action)
        {
            if (!result) // Using implicit bool operator!
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Execute action if Result<T> is successful
        /// </summary>
        public static Result<T> OnSuccess<T>(this Result<T> result, Action action)
        {
            if (result) // Using implicit bool operator!
                action();
            return result;
        }

        /// <summary>
        /// Execute action if Result<T> fails
        /// </summary>
        public static Result<T> OnFailure<T>(this Result<T> result, Action<IReadOnlyList<string>> action)
        {
            if (!result) // Using implicit bool operator!
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result<T>> is successful
        /// </summary>
        public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Action action)
        {
            var result = await resultTask;
            if (result) // Using implicit bool operator!
                action();
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result<T>> fails
        /// </summary>
        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result> is successful
        /// </summary>
        public static async Task<Result> OnSuccess(this Task<Result> resultTask, Action action)
        {
            var result = await resultTask;
            if (result) // Using implicit bool operator!
                action();
            return result;
        }

        /// <summary>
        /// Execute action if Task<Result> fails
        /// </summary>
        public static async Task<Result> OnFailure(this Task<Result> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                action(result.Errors);
            return result;
        }

        /// <summary>
        /// Transform Result<T> to Result<TNew> synchronously
        /// </summary>
        public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
        {
            if (!result) // Using implicit bool operator!
                return Result<TNew>.Failure(result.Errors);
            
            var newValue = mapper(result.Value!);
            return Result<TNew>.Success(newValue);
        }

        /// <summary>
        /// Transform Result<T> to Result<TNew> asynchronously
        /// </summary>
        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Result<T> result, Func<T, Task<TNew>> mapper)
        {
            if (!result) // Using implicit bool operator!
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
            if (!result) // Using implicit bool operator!
                return Result<TNew>.Failure(result.Errors);
            
            var newValue = await mapper(result.Value!);
            return Result<TNew>.Success(newValue);
        }

        /// <summary>
        /// Bind operation - chain Results together
        /// </summary>
        public static Result<TNew> Bind<T, TNew>(this Result<T> result, Func<T, Result<TNew>> func)
        {
            if (!result) // Using implicit bool operator!
                return Result<TNew>.Failure(result.Errors);
            
            return func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result<T> to Result
        /// </summary>
        public static Result Bind<T>(this Result<T> result, Func<T, Result> func)
        {
            if (!result) // Using implicit bool operator!
                return Result.Failure(result.Errors);
            
            return func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result to Result<T>
        /// </summary>
        public static Result<T> Bind<T>(this Result result, Func<Result<T>> func)
        {
            if (!result) // Using implicit bool operator!
                return Result<T>.Failure(result.Errors);
            
            return func();
        }

        /// <summary>
        /// Bind operation - chain Result to Result
        /// </summary>
        public static Result Bind(this Result result, Func<Result> func)
        {
            if (!result) // Using implicit bool operator!
                return result;
            
            return func();
        }

        /// <summary>
        /// Bind operation - chain Results together (async)
        /// </summary>
        public static async Task<Result<TNew>> BindAsync<T, TNew>(this Result<T> result, Func<T, Task<Result<TNew>>> func)
        {
            if (!result) // Using implicit bool operator!
                return Result<TNew>.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result<T> to Result (async)
        /// </summary>
        public static async Task<Result> BindAsync<T>(this Result<T> result, Func<T, Task<Result>> func)
        {
            if (!result) // Using implicit bool operator!
                return Result.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Result to Task<Result<T>>
        /// </summary>
        public static async Task<Result<T>> BindAsync<T>(this Result result, Func<Task<Result<T>>> func)
        {
            if (!result) // Using implicit bool operator!
                return Result<T>.Failure(result.Errors);
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Result to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync(this Result result, Func<Task<Result>> func)
        {
            if (!result) // Using implicit bool operator!
                return result;
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Task<Result<T>> to Task<Result<TNew>>
        /// </summary>
        public static async Task<Result<TNew>> BindAsync<T, TNew>(this Task<Result<T>> resultTask, Func<T, Task<Result<TNew>>> func)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                return Result<TNew>.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Task<Result<T>> to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<Result>> func)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                return Result.Failure(result.Errors);
            
            return await func(result.Value!);
        }

        /// <summary>
        /// Bind operation - chain Task<Result> to Task<Result<T>>
        /// </summary>
        public static async Task<Result<T>> BindAsync<T>(this Task<Result> resultTask, Func<Task<Result<T>>> func)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                return Result<T>.Failure(result.Errors);
            
            return await func();
        }

        /// <summary>
        /// Bind operation - chain Task<Result> to Task<Result>
        /// </summary>
        public static async Task<Result> BindAsync(this Task<Result> resultTask, Func<Task<Result>> func)
        {
            var result = await resultTask;
            if (!result) // Using implicit bool operator!
                return result;
            
            return await func();
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

        /// <summary>
        /// Convert Result<T> to Result (ignoring the value)
        /// Useful for update operations where you only care about success/failure
        /// </summary>
        public static Result ToResult<T>(this Result<T> result)
        {
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
        }

        /// <summary>
        /// Convert Task<Result<T>> to Task<Result> (ignoring the value)
        /// Useful for async update operations where you only care about success/failure
        /// </summary>
        public static async Task<Result> ToResultAsync<T>(this Task<Result<T>> resultTask)
        {
            var result = await resultTask;
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
        }

        /// <summary>
        /// Execute action on success and convert Result<T> to Result
        /// Useful for update operations where you want to perform an action but only return success/failure
        /// </summary>
        public static Result OnSuccessToResult<T>(this Result<T> result, Action<T> action)
        {
            if (result.IsSuccess)
            {
                action(result.Value!);
                return Result.Success();
            }
            return Result.Failure(result.Errors);
        }

        /// <summary>
        /// Execute async action on success and convert Result<T> to Result
        /// Useful for async update operations where you want to perform an action but only return success/failure
        /// </summary>
        public static async Task<Result> OnSuccessToResultAsync<T>(this Result<T> result, Func<T, Task> action)
        {
            if (result.IsSuccess)
            {
                await action(result.Value!);
                return Result.Success();
            }
            return Result.Failure(result.Errors);
        }

        /// <summary>
        /// Execute async action on Task<Result<T>> success and convert to Result
        /// Useful for async update operations where you want to perform an action but only return success/failure
        /// </summary>
        public static async Task<Result> OnSuccessToResultAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
        {
            var result = await resultTask;
            if (result.IsSuccess)
            {
                await action(result.Value!);
                return Result.Success();
            }
            return Result.Failure(result.Errors);
        }
    }
}

