using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId
{
    public class GetAccountsByUserIdQueryValidator : AbstractValidator<GetAccountsByUserIdQuery>
    {
        public GetAccountsByUserIdQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
        }
    }
}
