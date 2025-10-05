#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount
{
    public class UpdateSavingsAccountCommandValidator : AbstractValidator<UpdateSavingsAccountCommand>
    {
        public UpdateSavingsAccountCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Account id"));
            RuleFor(x => x.Req).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "CurrencyId"));
            });
        }
    }
}

