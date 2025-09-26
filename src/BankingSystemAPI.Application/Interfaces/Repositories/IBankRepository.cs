using BankingSystemAPI.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IBankRepository: IGenericRepository<Bank, int>
    {
        Task<Dictionary<int, string>> GetBankNamesByIdsAsync(IEnumerable<int> ids);
    }
}
