#region Usings
using FluentValidation;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetById
{
    public class GetTransactionByIdQueryValidator : AbstractValidator<GetTransactionByIdQuery>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public GetTransactionByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid transaction id.");
        }
    }
}

