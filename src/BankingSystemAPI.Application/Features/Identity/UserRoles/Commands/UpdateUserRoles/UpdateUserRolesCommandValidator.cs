using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {
        public UpdateUserRolesCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required.")
                .MaximumLength(50)
                .WithMessage("Role name cannot exceed 50 characters.");
        }
    }
}