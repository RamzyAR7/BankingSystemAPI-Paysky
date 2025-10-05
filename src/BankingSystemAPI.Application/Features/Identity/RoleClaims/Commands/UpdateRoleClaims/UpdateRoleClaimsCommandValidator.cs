#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims
{
    public sealed class UpdateRoleClaimsCommandValidator : AbstractValidator<UpdateRoleClaimsCommand>
    {
        public UpdateRoleClaimsCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role ID"));

            RuleFor(x => x.Claims)
                .NotNull()
                .WithMessage(ApiResponseMessages.Validation.ClaimsListRequired);
        }
    }
}
