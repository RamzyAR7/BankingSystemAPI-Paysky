using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(string UserId) : IQuery<UserResDto?>;
}