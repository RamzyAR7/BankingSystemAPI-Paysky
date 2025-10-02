using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims
{
    public sealed class UpdateRoleClaimsCommandValidator : AbstractValidator<UpdateRoleClaimsCommand>
    {
        public UpdateRoleClaimsCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage("Role ID is required.");

            RuleFor(x => x.Claims)
                .NotNull()
                .WithMessage("Claims collection cannot be null.");
        }
    }
}