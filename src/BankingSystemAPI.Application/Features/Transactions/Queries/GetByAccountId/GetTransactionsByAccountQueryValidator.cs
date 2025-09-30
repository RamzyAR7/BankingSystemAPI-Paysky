using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId
{
    public class GetTransactionsByAccountQueryValidator : AbstractValidator<GetTransactionsByAccountQuery>
    {
        public GetTransactionsByAccountQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0)
                .WithMessage("AccountId must be a positive integer.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be a positive integer.");
        }
    }
}
