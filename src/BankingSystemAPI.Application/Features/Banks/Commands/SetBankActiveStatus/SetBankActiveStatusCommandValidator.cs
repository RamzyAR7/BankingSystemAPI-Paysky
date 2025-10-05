#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus
{
    public class SetBankActiveStatusCommandValidator:AbstractValidator<SetBankActiveStatusCommand>
    {
        public SetBankActiveStatusCommandValidator()
        {
            RuleFor(x => x.id)
                .NotEmpty().WithMessage(ApiResponseMessages.Validation.InvalidIdFormat.Replace("{0}", "BankId"));

            RuleFor(x => x.isActive)
                .NotNull().WithMessage(ApiResponseMessages.Validation.RequiredDataFormat.Replace("{0}", "IsActive flag"));
        }
    }
}

