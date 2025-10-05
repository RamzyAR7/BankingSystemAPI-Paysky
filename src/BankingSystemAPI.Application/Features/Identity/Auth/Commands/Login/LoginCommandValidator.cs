#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Login
{
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Email"))
                .EmailAddress()
                .WithMessage(ApiResponseMessages.Validation.InvalidEmailAddress);

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Password"));
        }
    }
}
