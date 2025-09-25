using BankingSystemAPI.Domain.Entities;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IBankRepository: IGenericRepository<Bank, int>
    {
        Task<bool> HasUsersAsync(int bankId);
    }
}
