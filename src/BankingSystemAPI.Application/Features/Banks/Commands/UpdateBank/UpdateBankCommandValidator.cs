using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.UpdateBank
{
    public class UpdateBankCommandValidator: AbstractValidator<UpdateBankCommand>
    {
        public UpdateBankCommandValidator()
        {
            RuleFor(x => x.id)
                .GreaterThan(0).WithMessage("Bank ID must be greater than zero.");
            RuleFor(x => x.bankDto)
                .NotNull().WithMessage("Bank data must be provided.");
            When(x => x.bankDto != null, () =>
            {
                RuleFor(x => x.bankDto.Name)
                    .NotEmpty().WithMessage("Bank name must not be empty.")
                    .MaximumLength(200).WithMessage("Bank name must not exceed 200 characters.");
            });
        }
    }
}
