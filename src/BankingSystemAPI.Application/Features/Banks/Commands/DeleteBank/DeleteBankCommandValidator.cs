#region Usings
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank
{
    public class DeleteBankCommandValidator : AbstractValidator<DeleteBankCommand>
    {
        public DeleteBankCommandValidator()
        {
            RuleFor(x => x.id)
                .GreaterThan(0).WithMessage("Bank ID must be greater than zero.");
        }
    }
}

