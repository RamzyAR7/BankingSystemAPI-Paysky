using BankingSystemAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    // Generic interface for account-type specific services.
    // TCreateDto - DTO used when creating an account (may include InitialBalance)
    // TEditDto - DTO used when updating an account (must NOT include balance)
    // TResDto - DTO returned to clients
    public interface IAccountTypeService<TAccount, TCreateDto, TEditDto, TResDto> where TAccount : Account
    {
        Task<IEnumerable<TResDto>> GetAccountsAsync(int pageNumber, int pageSize);
        Task<TResDto> CreateAccountAsync(TCreateDto reqDto);
        Task<TResDto> UpdateAccountAsync(int accountId, TEditDto reqDto);
    }
}
