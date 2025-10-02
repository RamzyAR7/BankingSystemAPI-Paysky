using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
    {
        public ChangeUserPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.PasswordRequest)
                .NotNull()
                .WithMessage("Password request data is required.");

            // Validate ChangePasswordReqDto properties
            When(x => x.PasswordRequest != null, () =>
            {
                RuleFor(x => x.PasswordRequest.NewPassword)
                    .NotEmpty()
                    .WithMessage("New password is required.")
                    .MinimumLength(8)
                    .WithMessage("New password must be at least 8 characters long.")
                    .MaximumLength(100)
                    .WithMessage("New password cannot exceed 100 characters.")
                    .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
                    .WithMessage("New password must contain at least one lowercase letter, one uppercase letter, and one digit.");

                RuleFor(x => x.PasswordRequest.ConfirmNewPassword)
                    .NotEmpty()
                    .WithMessage("Password confirmation is required.")
                    .Equal(x => x.PasswordRequest.NewPassword)
                    .WithMessage("Password confirmation must match the new password.");

                // Current password validation is optional - business logic will determine if it's required
                RuleFor(x => x.PasswordRequest.CurrentPassword)
                    .MaximumLength(100)
                    .WithMessage("Current password cannot exceed 100 characters.")
                    .When(x => !string.IsNullOrEmpty(x.PasswordRequest.CurrentPassword));
            });
        }
    }
}