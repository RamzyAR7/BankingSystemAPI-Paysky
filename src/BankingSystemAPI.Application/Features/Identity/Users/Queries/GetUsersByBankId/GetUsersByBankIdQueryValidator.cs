#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed class GetUsersByBankIdQueryValidator : AbstractValidator<GetUsersByBankIdQuery>
    {
        public GetUsersByBankIdQueryValidator()
        {
            RuleFor(x => x.BankId)
                .GreaterThan(0)
                .WithMessage(ApiResponseMessages.Validation.BankIdGreaterThanZero);
        }
    }
}
