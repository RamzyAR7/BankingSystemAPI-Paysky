using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers
{
    public sealed record GetAllUsersQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? OrderBy = null,
        string? OrderDirection = null) : IQuery<IList<UserResDto>>;
}