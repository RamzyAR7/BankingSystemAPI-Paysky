using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Role name is required.")
                .MaximumLength(256)
                .WithMessage("Role name cannot exceed 256 characters.");
        }
    }
}