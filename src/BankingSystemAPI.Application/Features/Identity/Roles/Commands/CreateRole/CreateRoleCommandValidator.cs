#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role name"))
                .MaximumLength(256)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Role name", 256));
        }
    }
}
