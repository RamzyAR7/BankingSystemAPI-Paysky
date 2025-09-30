using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus
{
    public class SetBankActiveStatusCommandValidator:AbstractValidator<SetBankActiveStatusCommand>
    {
        public SetBankActiveStatusCommandValidator()
        {
            RuleFor(x => x.id)
                .NotEmpty().WithMessage("BankId is required");

            RuleFor(x => x.isActive)
                .NotNull().WithMessage("IsActive flag is required");
        }
    }
}
