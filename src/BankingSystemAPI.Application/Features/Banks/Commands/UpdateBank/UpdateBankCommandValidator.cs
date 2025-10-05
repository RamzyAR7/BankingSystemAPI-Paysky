#region Usings
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank
{
    public class UpdateBankCommandValidator: AbstractValidator<UpdateBankCommand>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public UpdateBankCommandValidator()
        {
            RuleFor(x => x.id)
                .GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Bank ID"));
            RuleFor(x => x.bankDto)
                .NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Bank data"));
            When(x => x.bankDto != null, () =>
            {
                RuleFor(x => x.bankDto.Name)
                    .NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Bank name"))
                    .MaximumLength(200).WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Bank name", 200));
            });
        }
    }
}

