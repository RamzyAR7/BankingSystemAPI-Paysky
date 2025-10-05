#region Usings
using System;
#endregion


namespace BankingSystemAPI.Application.Interfaces
{
    public interface ICacheService
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        bool TryGetValue<T>(object key, out T value);
        void Set<T>(object key , T value , TimeSpan? absoluteExpirationRelativeToNow = null);
        void Remove(object key);
    }
}

