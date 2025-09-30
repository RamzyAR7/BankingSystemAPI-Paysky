using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Common
{
    /// <summary>
    /// Standard operation result without a value.
    /// </summary>
    public class Result
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();

        public static Result Success() => new Result { Succeeded = true };
        public static Result Failure(IEnumerable<string> errors) => new Result { Succeeded = false, Errors = errors };
    }

    /// <summary>
    /// Standard operation result with a value.
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; set; }

        public static Result<T> Success(T value) => new Result<T> { Succeeded = true, Value = value };
        public static new Result<T> Failure(IEnumerable<string> errors) => new Result<T> { Succeeded = false, Errors = errors };
    }
}
