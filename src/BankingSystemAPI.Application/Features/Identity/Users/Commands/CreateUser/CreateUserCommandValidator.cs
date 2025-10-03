using FluentValidation;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.Identity;

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
                .WithMessage("User request cannot be null.");

            When(x => x.UserRequest != null, () =>
            {
                RuleFor(x => x.UserRequest.Username)
                    .NotEmpty()
                    .WithMessage("Username is required.")
                    .MaximumLength(50)
                    .WithMessage("Username cannot exceed 50 characters.");

                RuleFor(x => x.UserRequest.Email)
                    .NotEmpty()
                    .WithMessage("Email is required.")
                    .EmailAddress()
                    .WithMessage("Invalid email address.");

                RuleFor(x => x.UserRequest.Password)
                    .NotEmpty()
                    .WithMessage("Password is required.")
                    .MinimumLength(8)
                    .WithMessage("Password must be at least 8 characters long.");

                RuleFor(x => x.UserRequest.NationalId)
                    .NotEmpty()
                    .WithMessage("National ID is required.")
                    .Length(10, 20)
                    .WithMessage("National ID must be between 10 and 20 characters.")
                    .Matches(@"^\d+$")
                    .WithMessage("National ID must contain only digits.");

                RuleFor(x => x.UserRequest.FullName)
                    .NotEmpty()
                    .WithMessage("Full name is required.")
                    .MaximumLength(200)
                    .WithMessage("Full name cannot exceed 200 characters.")
                    .Matches(@"^[a-zA-Z\s]+$")
                    .WithMessage("Full name can only contain letters and spaces.");

                RuleFor(x => x.UserRequest.DateOfBirth)
                    .NotEmpty()
                    .WithMessage("Date of birth is required.")
                    .Must(BeValidAge)
                    .WithMessage("User must be at least 18 years old and not older than 100 years.");

                RuleFor(x => x.UserRequest.PhoneNumber)
                    .NotEmpty()
                    .WithMessage("Phone number is required.")
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage("Invalid phone number format. Must be 11-15 digits, optionally starting with +.");

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
                            
                            // Role is required only for SuperAdmin
                            if (isSuperAdmin)
                            {
                                return !string.IsNullOrWhiteSpace(role);
                            }
                            
                            // For non-SuperAdmin, role is optional (will be set to Client by handler)
                            return true;
                        }
                        catch
                        {
                            // If we can't determine user role, fail-safe by requiring role
                            return !string.IsNullOrWhiteSpace(role);
                        }
                    })
                    .WithMessage("Role is required for SuperAdmin users.")
                    .WithErrorCode("ROLE_REQUIRED_FOR_SUPERADMIN");

                // Format validation when role is provided
                RuleFor(x => x.UserRequest.Role)
                    .MaximumLength(100)
                    .WithMessage("Role name cannot exceed 100 characters.")
                    .Must(BeValidRoleFormat)
                    .WithMessage("Role name can only contain letters, numbers, and spaces.")
                    .When(x => !string.IsNullOrWhiteSpace(x.UserRequest.Role));
            }
            else
            {
                // Fallback: when no current user service, require role (backward compatibility)
                RuleFor(x => x.UserRequest.Role)
                    .NotEmpty()
                    .WithMessage("Role is required.")
                    .MaximumLength(100)
                    .WithMessage("Role name cannot exceed 100 characters.")
                    .Must(BeValidRoleFormat)
                    .WithMessage("Role name can only contain letters, numbers, and spaces.");
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
                                // SuperAdmin can specify any bank or no bank
                                return !bankId.HasValue || bankId.Value > 0;
                            }
                            else
                            {
                                // Non-SuperAdmin: BankId will be set by handler, so it's optional in request
                                return !bankId.HasValue || bankId.Value > 0;
                            }
                        }
                        catch
                        {
                            // Fail-safe: require valid BankId if provided
                            return !bankId.HasValue || bankId.Value > 0;
                        }
                    })
                    .WithMessage("Bank ID must be greater than 0 when provided.")
                    .WithErrorCode("INVALID_BANK_ID");
            }
            else
            {
                // Fallback: standard BankId validation
                RuleFor(x => x.UserRequest.BankId)
                    .GreaterThan(0)
                    .WithMessage("Bank ID must be greater than 0.")
                    .When(x => x.UserRequest.BankId.HasValue);
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
