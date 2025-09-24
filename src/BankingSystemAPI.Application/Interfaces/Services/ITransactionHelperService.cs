using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface ITransactionHelperService
    {
        Task<decimal> ConvertAsync(int fromCurrencyId, int toCurrencyId, decimal amount);
        Task<decimal> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount);
    }
}
