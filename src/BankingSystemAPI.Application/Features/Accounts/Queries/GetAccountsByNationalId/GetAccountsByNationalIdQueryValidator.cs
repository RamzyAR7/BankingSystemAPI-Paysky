#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId
{
    public class GetAccountsByNationalIdQueryValidator : AbstractValidator<GetAccountsByNationalIdQuery>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public GetAccountsByNationalIdQueryValidator()
        {
            RuleFor(x => x.NationalId)
                .NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "National id"))
                .Length(14).WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthRangeFormat, "National id", 14, 14))
                .Matches(@"^\d{14}$").WithMessage(ApiResponseMessages.Validation.NationalIdDigits);
        }
    }
}

