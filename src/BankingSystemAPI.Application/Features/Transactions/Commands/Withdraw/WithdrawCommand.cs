using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw
{
    public record WithdrawCommand(WithdrawReqDto Req) : ICommand<TransactionResDto>;
}
