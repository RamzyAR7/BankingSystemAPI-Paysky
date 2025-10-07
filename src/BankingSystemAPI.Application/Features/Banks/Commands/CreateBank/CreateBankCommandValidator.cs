#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Banks.Commands.CreateBank
{
    public class CreateBankCommandValidator : AbstractValidator<CreateBankCommand>
    {
        public CreateBankCommandValidator()
        {
            RuleFor(x => x.bankDto)
                .NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Bank data"));
            RuleFor(x => x.bankDto.Name)
                .NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Bank name"))
                .MaximumLength(200).WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Bank name", 200));
        }
    }
}

