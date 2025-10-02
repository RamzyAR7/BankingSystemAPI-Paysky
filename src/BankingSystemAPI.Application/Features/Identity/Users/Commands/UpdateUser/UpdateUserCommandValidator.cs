using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser
{
    public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.UserEdit)
                .NotNull()
                .WithMessage("User edit data is required.");

            // Validate UserEditDto properties
            When(x => x.UserEdit != null, () =>
            {
                RuleFor(x => x.UserEdit.Username)
                    .NotEmpty()
                    .WithMessage("Username is required.")
                    .MaximumLength(50)
                    .WithMessage("Username cannot exceed 50 characters.");

                RuleFor(x => x.UserEdit.Email)
                    .NotEmpty()
                    .WithMessage("Email is required.")
                    .EmailAddress()
                    .WithMessage("Invalid email address.");

                RuleFor(x => x.UserEdit.NationalId)
                    .NotEmpty()
                    .WithMessage("National ID is required.")
                    .Length(10, 20)
                    .WithMessage("National ID must be between 10 and 20 characters.")
                    .Matches(@"^\d+$")
                    .WithMessage("National ID must contain only digits.");

                RuleFor(x => x.UserEdit.FullName)
                    .NotEmpty()
                    .WithMessage("Full name is required.")
                    .MaximumLength(200)
                    .WithMessage("Full name cannot exceed 200 characters.")
                    .Matches(@"^[a-zA-Z\s]+$")
                    .WithMessage("Full name can only contain letters and spaces.");

                RuleFor(x => x.UserEdit.DateOfBirth)
                    .NotEmpty()
                    .WithMessage("Date of birth is required.")
                    .Must(BeValidAge)
                    .WithMessage("User must be at least 18 years old and not older than 100 years.");

                RuleFor(x => x.UserEdit.PhoneNumber)
                    .NotEmpty()
                    .WithMessage("Phone number is required.")
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage("Invalid phone number format. Must be 11-15 digits, optionally starting with +.");
            });
        }

        private bool BeValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;
            
            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= 18 && age <= 100;
        }
    }
}