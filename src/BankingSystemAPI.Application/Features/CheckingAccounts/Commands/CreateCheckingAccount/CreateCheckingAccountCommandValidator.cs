using FluentValidation;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount
{
    public class CreateCheckingAccountCommandValidator : AbstractValidator<CreateCheckingAccountCommand>
    {
        public CreateCheckingAccountCommandValidator()
        {
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.UserId).NotEmpty().WithMessage("UserId is required.");
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage("CurrencyId is required.");
                RuleFor(x => x.Req.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("InitialBalance must be non-negative.");
                RuleFor(x => x.Req.OverdraftLimit).GreaterThanOrEqualTo(0).WithMessage("OverdraftLimit must be non-negative.");
            });
        }
    }
}
