using BankingSystemAPI.Application.DTOs.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        Task<decimal> GetBalanceAsync(int accountId);
        Task<TransactionResDto> DepositAsync(DepositReqDto request);
        Task<TransactionResDto> WithdrawAsync(WithdrawReqDto request);
        Task<TransactionResDto> TransferAsync(TransferReqDto request);
        Task<IEnumerable<TransactionResDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<TransactionResDto> GetByIdAsync(int transactionId);
        Task<IEnumerable<TransactionResDto>> GetByAccountIdAsync(int accountId, int pageNumber, int pageSize);
    }
}
