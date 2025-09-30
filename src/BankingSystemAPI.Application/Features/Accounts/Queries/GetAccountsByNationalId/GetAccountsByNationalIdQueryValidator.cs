using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId
{
    public class GetAccountsByNationalIdQueryValidator : AbstractValidator<GetAccountsByNationalIdQuery>
    {
        public GetAccountsByNationalIdQueryValidator()
        {
            RuleFor(x => x.NationalId)
                .NotEmpty().WithMessage("National id is required.")
                .Length(14).WithMessage("National id must be exactly 14 digits.")
                .Matches(@"^\d{14}$").WithMessage("National id must contain only digits and be exactly 14 characters long.");
        }
    }
}
