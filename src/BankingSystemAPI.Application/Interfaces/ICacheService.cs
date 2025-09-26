using System;

namespace BankingSystemAPI.Application.Interfaces
{
    public interface ICacheService
    {
        bool TryGetValue<T>(object key, out T value);
        void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null);
        T GetOrCreate<T>(object key, Func<T> factory, TimeSpan? absoluteExpirationRelativeToNow = null);
        void Remove(object key);
    }
}
