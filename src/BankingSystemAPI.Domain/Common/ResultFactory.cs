#region Usings
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

namespace BankingSystemAPI.Domain.Common
{
    /// <summary>
    /// Helper to construct Result and Result{T} instances via reflection in a centralized place.
    /// This isolates reflection usage and simplifies testing.
    /// </summary>
    public static class ResultFactory
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo?> _cachedFailureMethod = new();

        /// <summary>
        /// Create a closed generic Result{T} failure instance by invoking the static Failure(IEnumerable<ResultError>) method.
        /// Returns the instance as object which can be cast by the caller.
        /// </summary>
        public static object? CreateGenericFailure(Type genericArg, IEnumerable<ResultError> errors)
        {
            if (genericArg == null) throw new ArgumentNullException(nameof(genericArg));
            var openGeneric = typeof(Result<>);
            var closed = openGeneric.MakeGenericType(genericArg);

            var method = _cachedFailureMethod.GetOrAdd(closed, t =>
            {
                // Look for Failure(IEnumerable<ResultError>) static method
                return t.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(IEnumerable<ResultError>) }, null);
            });

            if (method == null) return null;
            return method.Invoke(null, new object[] { errors });
        }
    }
}
