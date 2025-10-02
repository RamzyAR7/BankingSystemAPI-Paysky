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

            RuleFor(x => x.Roles)
                .NotNull()
                .WithMessage("Roles collection cannot be null.");
        }
    }
}