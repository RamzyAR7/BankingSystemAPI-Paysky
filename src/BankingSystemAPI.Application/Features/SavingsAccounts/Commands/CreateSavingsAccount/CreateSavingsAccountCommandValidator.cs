using FluentValidation;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount
{
    public class CreateSavingsAccountCommandValidator : AbstractValidator<CreateSavingsAccountCommand>
    {
        public CreateSavingsAccountCommandValidator()
        {
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.UserId).NotEmpty().WithMessage("UserId is required.");
                RuleFor(x => x.Req.CurrencyId).GreaterThan(0).WithMessage("CurrencyId is required.");
                RuleFor(x => x.Req.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("Initial balance must be zero or greater.");
            });
        }
    }
}
