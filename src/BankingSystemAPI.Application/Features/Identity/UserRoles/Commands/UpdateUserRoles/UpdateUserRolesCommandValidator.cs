#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {

        public UpdateUserRolesCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "User ID"));

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role"))
                .MaximumLength(50)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Role name", 50));
        }
    }
}
