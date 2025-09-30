using FluentValidation;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllInterestLogs
{
    public class GetAllInterestLogsQueryValidator : AbstractValidator<GetAllInterestLogsQuery>
    {
        public GetAllInterestLogsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0);
        }
    }
}
