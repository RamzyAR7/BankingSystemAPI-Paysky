using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankById
{
    public class GetBankByIdQueryValidator
        : AbstractValidator<GetBankByIdQuery>
    {
        public GetBankByIdQueryValidator()
        {
            RuleFor(x => x.id)
                .NotEmpty().WithMessage("BankId is required");
        }
    }
}
