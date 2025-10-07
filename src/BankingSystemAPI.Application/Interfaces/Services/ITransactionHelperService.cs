#region Usings
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface ITransactionHelperService
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        Task<decimal> ConvertAsync(int fromCurrencyId, int toCurrencyId, decimal amount);
        Task<decimal> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount);
    }
}

