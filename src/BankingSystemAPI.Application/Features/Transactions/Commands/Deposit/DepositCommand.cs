using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Deposit
{
    public record DepositCommand(DepositReqDto Req) : ICommand<TransactionResDto>;
}
