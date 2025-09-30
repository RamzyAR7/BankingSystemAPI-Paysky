using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetById
{
    public record GetTransactionByIdQuery(int Id) : IQuery<TransactionResDto>;
}
