#region Usings
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankByName
{
    public class GetBankByNameQueryValidator: AbstractValidator<GetBankByNameQuery>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public GetBankByNameQueryValidator()
        {
            RuleFor(x => x.name)
                .NotEmpty().WithMessage("Bank name must not be empty.")
                .MaximumLength(200).WithMessage("Bank name must not exceed 100 characters.");
        }
    }
}

