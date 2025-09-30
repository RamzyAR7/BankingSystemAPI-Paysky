using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById
{
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        public GetAccountByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid account id.");
        }
    }
}
