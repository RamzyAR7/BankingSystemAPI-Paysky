using FluentValidation;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount
{
    public class UpdateCheckingAccountCommandValidator : AbstractValidator<UpdateCheckingAccountCommand>
    {
        public UpdateCheckingAccountCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid account id.");
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.UserId).NotEmpty().WithMessage("UserId is required.");
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage("CurrencyId is required.");
                RuleFor(x => x.Req.OverdraftLimit).GreaterThanOrEqualTo(0).WithMessage("OverdraftLimit must be non-negative.");
            });
        }
    }
}
