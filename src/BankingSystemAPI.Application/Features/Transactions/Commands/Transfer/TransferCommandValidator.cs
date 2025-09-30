using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Commands.Transfer
{
    public class TransferCommandValidator : AbstractValidator<TransferCommand>
    {
        public TransferCommandValidator()
        {
            RuleFor(x => x.Req)
                .NotNull()
                .WithMessage("Request body is required.");

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.SourceAccountId)
                    .GreaterThan(0)
                    .WithMessage("SourceAccountId must be greater than 0.");

                RuleFor(x => x.Req.TargetAccountId)
                    .GreaterThan(0)
                    .WithMessage("TargetAccountId must be greater than 0.");

                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0)
                    .WithMessage("Amount must be greater than 0.");

                // Ensure source and target accounts are not the same
                RuleFor(x => x.Req.TargetAccountId)
                    .NotEqual(x => x.Req.SourceAccountId)
                    .WithMessage("Source and target account IDs must be different.");
            });
        }
    }
}
