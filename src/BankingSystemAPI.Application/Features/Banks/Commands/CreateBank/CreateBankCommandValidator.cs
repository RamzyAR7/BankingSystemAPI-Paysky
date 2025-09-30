using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.CreateBank
{
    public class CreateBankCommandValidator: AbstractValidator<CreateBankCommand>
    {
        public CreateBankCommandValidator()
        {
            RuleFor(x => x.bankDto)
                .NotNull().WithMessage("Bank data must be provided.");
            RuleFor(x => x.bankDto.Name)
                .NotEmpty().WithMessage("Bank name is required.")
                .MaximumLength(200).WithMessage("Bank name must not exceed 200 characters.");
        }
    }
}
