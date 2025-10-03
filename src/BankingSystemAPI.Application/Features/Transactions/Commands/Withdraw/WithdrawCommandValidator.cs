using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Withdraw
{
    /// <summary>
    /// Simplified withdraw validator with basic input validation only
    /// Business logic validation handled in the handler
    /// </summary>
    public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
    {
        public WithdrawCommandValidator()
        {
            RuleFor(x => x.Req).NotNull().WithMessage("Request body is required.");

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0).WithMessage("Withdrawal amount must be greater than zero.");

                RuleFor(x => x.Req.AccountId)
                    .GreaterThan(0).WithMessage("Account ID is required.");
            });
        }
    }
}
