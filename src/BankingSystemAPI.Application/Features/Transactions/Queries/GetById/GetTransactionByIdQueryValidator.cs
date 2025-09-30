using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetById
{
    public class GetTransactionByIdQueryValidator : AbstractValidator<GetTransactionByIdQuery>
    {
        public GetTransactionByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid transaction id.");
        }
    }
}
