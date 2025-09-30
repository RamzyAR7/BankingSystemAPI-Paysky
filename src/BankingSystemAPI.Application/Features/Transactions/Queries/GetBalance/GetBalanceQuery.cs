using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance
{
    public record GetBalanceQuery(int AccountId) : IQuery<decimal>;
}
