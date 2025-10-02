using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed record GetUsersByBankIdQuery(int BankId) : IQuery<IList<UserResDto>>;
}