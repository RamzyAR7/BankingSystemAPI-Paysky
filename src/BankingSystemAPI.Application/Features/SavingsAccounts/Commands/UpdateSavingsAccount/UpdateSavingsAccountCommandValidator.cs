using FluentValidation;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount
{
    public class UpdateSavingsAccountCommandValidator : AbstractValidator<UpdateSavingsAccountCommand>
    {
        public UpdateSavingsAccountCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid account id.");
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage("CurrencyId is required.");
            });
        }
    }
}
