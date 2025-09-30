using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllInterestLogs
{
    public record GetAllInterestLogsQuery(int PageNumber = 1, int PageSize = 10, string? OrderBy = null, string? OrderDirection = null) : IQuery<InterestLogsPagedDto>;
}
