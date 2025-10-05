#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount
{
    public class UpdateCheckingAccountCommandValidator : AbstractValidator<UpdateCheckingAccountCommand>
    {
        public UpdateCheckingAccountCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Account id"));
            RuleFor(x => x.Req).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.UserId).NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "UserId"));
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "CurrencyId"));
                RuleFor(x => x.Req.OverdraftLimit).GreaterThanOrEqualTo(0).WithMessage(ApiResponseMessages.Validation.OverdraftLimitNonNegative);
            });
        }
    }
}

