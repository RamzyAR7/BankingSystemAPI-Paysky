#region Usings
using FluentValidation;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance
{
    public class GetBalanceQueryValidator : AbstractValidator<GetBalanceQuery>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public GetBalanceQueryValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("Invalid account id.");
        }
    }
}

