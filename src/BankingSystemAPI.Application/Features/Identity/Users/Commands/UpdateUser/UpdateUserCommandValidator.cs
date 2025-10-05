#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser
{
    public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "User ID"));

            RuleFor(x => x.UserEdit)
                .NotNull()
                .WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "User edit data"));

            // Validate UserEditDto properties
            When(x => x.UserEdit != null, () =>
            {
                RuleFor(x => x.UserEdit.Username)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Username"))
                    .MaximumLength(50)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Username", 50));

                RuleFor(x => x.UserEdit.Email)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Email"))
                    .EmailAddress()
                    .WithMessage(ApiResponseMessages.Validation.InvalidEmailAddress);

                RuleFor(x => x.UserEdit.NationalId)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "National ID"))
                    .Length(10, 20)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthRangeFormat, "National ID", 10, 20))
                    .Matches(@"^\d+$")
                    .WithMessage(ApiResponseMessages.Validation.NationalIdDigits);

                RuleFor(x => x.UserEdit.FullName)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Full name"))
                    .MaximumLength(200)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Full name", 200))
                    .Matches(@"^[a-zA-Z\s]+$")
                    .WithMessage(ApiResponseMessages.Validation.FullNameLettersOnly);

                RuleFor(x => x.UserEdit.DateOfBirth)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Date of birth"))
                    .Must(BeValidAge)
                    .WithMessage(ApiResponseMessages.Validation.AgeRange);

                RuleFor(x => x.UserEdit.PhoneNumber)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Phone number"))
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage(ApiResponseMessages.Validation.InvalidPhoneNumberFormat);
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
