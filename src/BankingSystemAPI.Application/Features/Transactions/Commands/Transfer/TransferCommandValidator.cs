#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Commands.Transfer
{
    /// <summary>
    /// Simplified transfer validator with basic input validation only
    /// Business logic validation handled in the handler
    /// </summary>
    public class TransferCommandValidator : AbstractValidator<TransferCommand>
    {
        public TransferCommandValidator()
        {
            RuleFor(x => x.Req)
                .NotNull()
                .WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.SourceAccountId)
                    .GreaterThan(0)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "SourceAccountId"));

                RuleFor(x => x.Req.TargetAccountId)
                    .GreaterThan(0)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "TargetAccountId"));

                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0)
                    .WithMessage(ApiResponseMessages.Validation.TransferAmountGreaterThanZero);

                // Ensure source and target accounts are not the same
                RuleFor(x => x.Req.TargetAccountId)
                    .NotEqual(x => x.Req.SourceAccountId)
                    .WithMessage(ApiResponseMessages.Validation.SourceAndTargetAccountsMustDiffer);
            });
        }
    }
}
