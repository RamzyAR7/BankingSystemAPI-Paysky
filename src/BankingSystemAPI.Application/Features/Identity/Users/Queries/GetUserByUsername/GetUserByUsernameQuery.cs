using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserByUsername
{
    public sealed record GetUserByUsernameQuery(string Username) : IQuery<UserResDto>;
}