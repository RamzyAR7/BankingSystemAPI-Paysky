using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Deposit
{
    /// <summary>
    /// Simplified deposit validator with basic input validation only
    /// Business logic validation handled in the handler
    /// </summary>
    public class DepositCommandValidator : AbstractValidator<DepositCommand>
    {
        public DepositCommandValidator()
        {
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0).WithMessage("Deposit amount must be greater than zero.");

                RuleFor(x => x.Req.AccountId)
                    .GreaterThan(0).WithMessage("Account ID is required.");
            });
        }
    }
}
