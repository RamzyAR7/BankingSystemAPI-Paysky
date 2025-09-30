using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankByName
{
    public class GetBankByNameQueryValidator: AbstractValidator<GetBankByNameQuery>
    {
        public GetBankByNameQueryValidator()
        {
            RuleFor(x => x.name)
                .NotEmpty().WithMessage("Bank name must not be empty.")
                .MaximumLength(200).WithMessage("Bank name must not exceed 100 characters.");
        }
    }
}
