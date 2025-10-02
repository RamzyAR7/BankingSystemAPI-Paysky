using FluentValidation;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Services;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.DependencyInjection;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser
{
    /// <summary>
    /// Validator for CreateUserCommand that uses UserReqDto
    /// </summary>
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly IServiceProvider? _serviceProvider;

        public CreateUserCommandValidator(IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider;

            // Validate that UserRequest is not null
            RuleFor(x => x.UserRequest)
                .NotNull().WithMessage("User request data is required.");

            // Validate UserReqDto properties
            When(x => x.UserRequest != null, () =>
            {
                ConfigureBasicValidation();
                
                // Business validation - only if services are available
                if (_serviceProvider != null)
                {
                    ConfigureBusinessValidation();
                }
            });
        }

        private void ConfigureBasicValidation()
        {
            RuleFor(x => x.UserRequest.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

            RuleFor(x => x.UserRequest.Username)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
                .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens.");

            RuleFor(x => x.UserRequest.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("Full name can only contain letters and spaces.");

            RuleFor(x => x.UserRequest.NationalId)
                .NotEmpty().WithMessage("National ID is required.")
                .Length(10, 20).WithMessage("National ID must be between 10 and 20 characters.")
                .Matches(@"^\d+$").WithMessage("National ID must contain only digits.");

            RuleFor(x => x.UserRequest.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format. Must be 11-15 digits, optionally starting with +.");

            RuleFor(x => x.UserRequest.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.")
                .Must(BeValidAge).WithMessage("User must be at least 18 years old and not older than 100 years.");

            RuleFor(x => x.UserRequest.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit.");

            RuleFor(x => x.UserRequest.PasswordConfirm)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.UserRequest.Password).WithMessage("Password confirmation must match the password.");

            RuleFor(x => x.UserRequest.BankId)
                .GreaterThan(0).WithMessage("Bank ID must be greater than 0.")
                .When(x => x.UserRequest.BankId.HasValue);

            RuleFor(x => x.UserRequest.Role)
                .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.UserRequest.Role));
        }

        private bool BeValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;
            
            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= 18 && age <= 100;
        }

        private void ConfigureBusinessValidation()
        {
            // Business validation - Bank exists and is active (for SuperAdmin)
            RuleFor(x => x.UserRequest.BankId)
                .MustAsync(async (command, bankId, cancellation) =>
                {
                    if (!bankId.HasValue) return true;

                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var validationService = scope.ServiceProvider.GetService<IValidationService>();
                        if (validationService == null) return true;

                        var result = await validationService.ValidateBankAsync(bankId.Value);
                        return result.Succeeded;
                    }
                    catch
                    {
                        return true;
                    }
                })
                .WithMessage("Bank not found or inactive.")
                .When(x => x.UserRequest.BankId.HasValue);

            // Business validation - Role exists and is valid (for SuperAdmin)
            RuleFor(x => x.UserRequest.Role)
                .MustAsync(async (command, role, cancellation) =>
                {
                    if (string.IsNullOrWhiteSpace(role)) return true;

                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
                        if (unitOfWork == null) return true;

                        var roles = await unitOfWork.RoleRepository.ListAsync(new Application.Specifications.AllRolesSpecification());
                        var validRole = roles.Any(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase));
                        return validRole && !role.Equals(UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return true;
                    }
                })
                .WithMessage("Invalid or forbidden role specified.")
                .When(x => !string.IsNullOrWhiteSpace(x.UserRequest.Role));

            // Business validation - No duplicate users
            RuleFor(x => x.UserRequest)
                .MustAsync(async (command, userRequest, cancellation) =>
                {
                    if (!userRequest.BankId.HasValue) return true;

                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
                        if (unitOfWork == null) return true;

                        var existingUsers = await unitOfWork.UserRepository.GetUsersByBankIdAsync(userRequest.BankId.Value);
                        return !existingUsers.Any(u =>
                            string.Equals(u.UserName, userRequest.Username, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(u.Email, userRequest.Email, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(u.NationalId, userRequest.NationalId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(u.PhoneNumber, userRequest.PhoneNumber, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return true;
                    }
                })
                .WithMessage("A user with the same username, email, national ID, or phone number already exists in this bank.")
                .When(x => x.UserRequest.BankId.HasValue);
        }
    }
}