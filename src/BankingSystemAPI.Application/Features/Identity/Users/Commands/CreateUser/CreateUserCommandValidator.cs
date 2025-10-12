#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Identity;
using static BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser.CreateUserCommand;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser
{
    /// <summary>
    /// Simplified user creation validator with role-based validation
    /// Role field is only required for SuperAdmin users
    /// </summary>
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly ICurrentUserService? _currentUserService;

        public CreateUserCommandValidator(ICurrentUserService? currentUserService = null)
        {
            _currentUserService = currentUserService;
            ConfigureValidationRules();
        }

        private void ConfigureValidationRules()
        {
            RuleFor(x => x.UserRequest)
                .NotNull()
                .WithMessage(ApiResponseMessages.Validation.UserRequestCannotBeNull);

            When(x => x.UserRequest != null, () =>
            {
                RuleFor(x => x.UserRequest.Username)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Username"))
                    .MaximumLength(50)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Username", 50));

                RuleFor(x => x.UserRequest.Email)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Email"))
                    .EmailAddress()
                    .WithMessage(ApiResponseMessages.Validation.InvalidEmailAddress);

                RuleFor(x => x.UserRequest.Password)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Password"))
                    .MinimumLength(8)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMinFormat, "Password", 8));

                RuleFor(x => x.UserRequest.NationalId)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "National ID"))
                    .Length(10, 20)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthRangeFormat, "National ID", 10, 20))
                    .Matches(@"^\d+$")
                    .WithMessage(ApiResponseMessages.Validation.NationalIdDigits);

                RuleFor(x => x.UserRequest.FullName)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Full name"))
                    .MaximumLength(200)
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Full name", 200))
                    .Matches(@"^[a-zA-Z\s]+$")
                    .WithMessage(ApiResponseMessages.Validation.FullNameLettersOnly);

                RuleFor(x => x.UserRequest.DateOfBirth)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Date of birth"))
                    .Must(BeValidAge)
                    .WithMessage(ApiResponseMessages.Validation.AgeRange);

                RuleFor(x => x.UserRequest.PhoneNumber)
                    .NotEmpty()
                    .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Phone number"))
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage(ApiResponseMessages.Validation.InvalidPhoneNumberFormat);

                // Role validation - only required for SuperAdmin users
                ConfigureRoleValidation();

                // BankId validation - only required for SuperAdmin users
                ConfigureBankIdValidation();
            });
        }

        private void ConfigureRoleValidation()
        {
            if (_currentUserService != null)
            {
                // When current user service is available, check if user is SuperAdmin
                RuleFor(x => x.UserRequest.Role)
                    .MustAsync(async (command, role, cancellationToken) =>
                    {
                        try
                        {
                            var currentUserRole = await _currentUserService.GetRoleFromStoreAsync();
                            var isSuperAdmin = string.Equals(currentUserRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);

                            // SuperAdmin must provide a role.
                            if (isSuperAdmin)
                                return !string.IsNullOrWhiteSpace(role);

                            // Non-SuperAdmin must NOT set a role in the request (handler assigns Client role).
                            return string.IsNullOrWhiteSpace(role);
                        }
                        catch
                        {
                            // If we can't determine user role, fail-safe by requiring role
                            return !string.IsNullOrWhiteSpace(role);
                        }
                    })
                    .WithMessage(ApiResponseMessages.Validation.RoleRequiredForSuperAdmin)
                    .WithErrorCode("ROLE_REQUIRED_FOR_SUPERADMIN");

                // Format validation when role is provided
                RuleFor(x => x.UserRequest.Role)
                    .MaximumLength(100)
                    .WithMessage(ApiResponseMessages.Validation.RoleNameCannotExceed)
                    .Must(BeValidRoleFormat)
                    .WithMessage(ApiResponseMessages.Validation.RoleNameInvalidFormat)
                    .When(x => !string.IsNullOrWhiteSpace(x.UserRequest.Role));
            }
            else
            {
                // Fallback: when no current user service, do not enforce role requirement here.
                // Role existence and assignment will be validated/assigned by the handler.
                RuleFor(x => x.UserRequest.Role)
                    .MaximumLength(100)
                    .WithMessage(ApiResponseMessages.Validation.RoleNameCannotExceed)
                    .Must(BeValidRoleFormat)
                    .When(x => !string.IsNullOrWhiteSpace(x.UserRequest.Role))
                    .WithMessage(ApiResponseMessages.Validation.RoleNameInvalidFormat);
            }
        }

        private void ConfigureBankIdValidation()
        {
            if (_currentUserService != null)
            {
                // BankId validation is also context-dependent
                RuleFor(x => x.UserRequest.BankId)
                    .MustAsync(async (command, bankId, cancellationToken) =>
                    {
                        try
                        {
                            var currentUserRole = await _currentUserService.GetRoleFromStoreAsync();
                            var isSuperAdmin = string.Equals(currentUserRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);

                            // BankId validation based on user role
                            if (isSuperAdmin)
                            {
                                // SuperAdmin must provide a valid BankId
                                return bankId.HasValue && bankId.Value > 0;
                            }
                            else
                            {
                                // Non-SuperAdmin must NOT specify BankId in the request (handler will set it)
                                return !bankId.HasValue;
                            }
                        }
                        catch
                        {
                            // Fail-safe: require valid BankId if provided
                            return bankId.HasValue && bankId.Value > 0;
                        }
                    })
                    .WithMessage(ApiResponseMessages.Validation.BankIdMustBeGreaterThanZero)
                    .WithErrorCode("INVALID_BANK_ID");
            }
            else
            {
                // Fallback: when no current user service is available, do not strictly require BankId here.
                // Handlers will set BankId for non-SuperAdmin flows; validators should be tolerant in this case.
                // However keep a lightweight check if a BankId is provided.
                RuleFor(x => x.UserRequest.BankId)
                    .GreaterThan(0)
                    .WithMessage(ApiResponseMessages.Validation.BankIdMustBeGreaterThanZero)
                    .When(x => x.UserRequest.BankId.HasValue && x.UserRequest.BankId.Value != 0);
            }
        }

        private bool BeValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= 18 && age <= 100;
        }

        /// <summary>
        /// Validates role name format - allows any reasonable role name
        /// Business logic in handler will validate if the role actually exists in the system
        /// </summary>
        private bool BeValidRoleFormat(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            // Allow letters, numbers, and spaces - no special characters that could cause issues
            return role.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) &&
                   !string.IsNullOrWhiteSpace(role.Trim());
        }
    }
}
