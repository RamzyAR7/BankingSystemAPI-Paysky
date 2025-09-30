using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Deposit
{
    public class DepositCommandValidator : AbstractValidator<DepositCommand>
    {
        public DepositCommandValidator()
        {
            RuleFor(x => x.Req).NotNull();
            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.AccountId).GreaterThan(0);
                RuleFor(x => x.Req.Amount).GreaterThan(0);
            });
        }
    }
}
