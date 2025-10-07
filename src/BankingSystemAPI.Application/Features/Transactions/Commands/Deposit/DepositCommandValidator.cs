#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Commands.Deposit
{
    /// <summary>
    /// Simplified deposit validator with basic input validation only
    /// Business logic validation handled in the handler
    /// </summary>
    public class DepositCommandValidator : AbstractValidator<DepositCommand>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public DepositCommandValidator()
        {
            RuleFor(x => x.Req).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));

            When(x => x.Req != null, () =>
            {
                RuleFor(x => x.Req.Amount)
                    .GreaterThan(0).WithMessage(ApiResponseMessages.Validation.DepositAmountGreaterThanZero);

                RuleFor(x => x.Req.AccountId)
                    .GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Account ID"));
            });
        }
    }
}

