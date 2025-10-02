using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed class GetUsersByBankIdQueryValidator : AbstractValidator<GetUsersByBankIdQuery>
    {
        public GetUsersByBankIdQueryValidator()
        {
            RuleFor(x => x.BankId)
                .GreaterThan(0)
                .WithMessage("Bank ID must be greater than 0.");
        }
    }
}