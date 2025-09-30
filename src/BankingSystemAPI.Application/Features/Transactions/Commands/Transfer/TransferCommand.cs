using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Transfer
{
    public record TransferCommand(TransferReqDto Req) : ICommand<TransactionResDto>;
}
