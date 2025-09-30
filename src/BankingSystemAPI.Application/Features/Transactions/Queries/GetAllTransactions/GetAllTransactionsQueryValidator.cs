using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions
{
    public class GetAllTransactionsQueryValidator : AbstractValidator<GetAllTransactionsQuery>
    {
        public GetAllTransactionsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0);
        }
    }
}
