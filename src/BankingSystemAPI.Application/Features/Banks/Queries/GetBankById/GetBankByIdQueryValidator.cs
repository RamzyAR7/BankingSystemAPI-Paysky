#region Usings
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankById
{
    public class GetBankByIdQueryValidator
        : AbstractValidator<GetBankByIdQuery>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public GetBankByIdQueryValidator()
        {
            RuleFor(x => x.id)
                .NotEmpty().WithMessage("BankId is required");
        }
    }
}

