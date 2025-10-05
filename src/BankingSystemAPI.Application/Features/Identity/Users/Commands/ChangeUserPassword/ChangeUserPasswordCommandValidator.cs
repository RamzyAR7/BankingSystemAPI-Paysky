#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
    {
        public ChangeUserPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "User ID"));

            RuleFor(x => x.PasswordRequest)
                .NotNull()
                .WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Password request data"));

            // Validate ChangePasswordReqDto properties
            When(x => x.PasswordRequest != null, () =>
            {
                RuleFor(x => x.PasswordRequest.NewPassword)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "New password"))
                    .MinimumLength(8)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMinFormat, "New password", 8))
                    .MaximumLength(100)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "New password", 100))
                    .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
                    .WithMessage(ApiResponseMessages.Validation.PasswordComplexity);

                RuleFor(x => x.PasswordRequest.ConfirmNewPassword)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Password confirmation"))
                    .Equal(x => x.PasswordRequest.NewPassword)
                    .WithMessage(ApiResponseMessages.Validation.PasswordConfirmationMismatch);

                // Current password validation is optional - business logic will determine if it's required
                RuleFor(x => x.PasswordRequest.CurrentPassword)
                    .MaximumLength(100)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Current password", 100))
                    .When(x => !string.IsNullOrEmpty(x.PasswordRequest.CurrentPassword));
            });
        }
    }
}
