#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


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
            RuleFor(x => x.Req).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0).WithMessage(ApiResponseMessages.Validation.TransferAmountGreaterThanZero);

                RuleFor(x => x.Req.AccountId)
                    .GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Account ID"));
            });
        }
    }
}

