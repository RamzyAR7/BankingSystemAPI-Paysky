#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
#endregion

namespace BankingSystemAPI.Domain.Extensions
{
    public static class ResultExtensions
    {
        public static Result OnSuccess(this Result result, Action action)
        {
            if (result is { IsSuccess: true })
                action();
            return result;
        }

        public static Result OnFailure(this Result result, Action<IReadOnlyList<string>> action)
        {
            if (result is { IsSuccess: false })
                action(result.Errors);
            return result;
        }

        public static Result OnFailureErrors(this Result result, Action<IReadOnlyList<ResultError>> action)
        {
            if (result is { IsSuccess: false })
                action(result.ErrorItems);
            return result;
        }

        public static Result<T> OnSuccess<T>(this Result<T> result, Action action)
        {
            if (result is { IsSuccess: true })
                action();
            return result;
        }

        public static Result<T> OnFailure<T>(this Result<T> result, Action<IReadOnlyList<string>> action)
        {
            if (result is { IsSuccess: false })
                action(result.Errors);
            return result;
        }

        public static Result<T> OnFailureErrors<T>(this Result<T> result, Action<IReadOnlyList<ResultError>> action)
        {
            if (result is { IsSuccess: false })
                action(result.ErrorItems);
            return result;
        }

        public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Action action)
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.OnSuccess(action);
        }

        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.OnFailure(action);
        }

        public static async Task<Result> OnSuccess(this Task<Result> resultTask, Action action)
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.OnSuccess(action);
        }

        public static async Task<Result> OnFailure(this Task<Result> resultTask, Action<IReadOnlyList<string>> action)
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.OnFailure(action);
        }

        public static Result Then(this Result result, Func<Result> func) => result.IsFailure ? result : func();

        public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
        {
            if (result.IsFailure)
                return Result<TNew>.Failure(result.ErrorItems);
            return Result<TNew>.Success(mapper(result.Value!));
        }

        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Result<T> result, Func<T, Task<TNew>> mapper)
        {
            if (result.IsFailure) return Result<TNew>.Failure(result.ErrorItems);
            return Result<TNew>.Success(await mapper(result.Value!).ConfigureAwait(false));
        }

        public static async Task<Result<TNew>> MapAsync<T, TNew>(this Task<Result<T>> resultTask, Func<T, Task<TNew>> mapper)
        {
            var r = await resultTask.ConfigureAwait(false);
            return await r.MapAsync(mapper).ConfigureAwait(false);
        }

        public static Result<TNew> Bind<T, TNew>(this Result<T> result, Func<T, Result<TNew>> func)
        {
            if (result.IsFailure) return Result<TNew>.Failure(result.ErrorItems);
            return func(result.Value!);
        }

        public static Result Bind<T>(this Result<T> result, Func<T, Result> func)
        {
            if (result.IsFailure) return Result.Failure(result.ErrorItems);
            return func(result.Value!);
        }

        public static async Task<Result<TNew>> BindAsync<T, TNew>(this Task<Result<T>> resultTask, Func<T, Task<Result<TNew>>> func)
        {
            var r = await resultTask.ConfigureAwait(false);
            if (r.IsFailure) return Result<TNew>.Failure(r.ErrorItems);
            return await func(r.Value!).ConfigureAwait(false);
        }

        public static async Task<Result> BindAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<Result>> func)
        {
            var r = await resultTask.ConfigureAwait(false);
            if (r.IsFailure) return Result.Failure(r.ErrorItems);
            return await func(r.Value!).ConfigureAwait(false);
        }

        public static Result ValidateAll(params Result[] results) => Result.Combine(results);

        public static Result<T> ToResult<T>(this T? value, string errorMessage) where T : class
        {
            return value == null ? Result<T>.Failure(new ResultError(ErrorType.Validation, errorMessage)) : Result<T>.Success(value);
        }

        public static TResult Match<TResult>(this Result result, Func<TResult> onSuccess, Func<IReadOnlyList<ResultError>, TResult> onFailure)
        {
            return result.IsSuccess ? onSuccess() : onFailure(result.ErrorItems);
        }

        public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> onSuccess, Func<IReadOnlyList<ResultError>, TResult> onFailure)
        {
            return result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.ErrorItems);
        }
    }
}
