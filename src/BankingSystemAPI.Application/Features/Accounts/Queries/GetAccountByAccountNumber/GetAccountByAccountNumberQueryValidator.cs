#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber
{
    public class GetAccountByAccountNumberQueryValidator : AbstractValidator<GetAccountByAccountNumberQuery>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public GetAccountByAccountNumberQueryValidator()
        {
            RuleFor(x => x.AccountNumber).NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Account number"));
        }
    }
}

