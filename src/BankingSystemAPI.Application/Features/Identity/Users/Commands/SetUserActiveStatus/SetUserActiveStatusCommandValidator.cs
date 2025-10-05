#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.SetUserActiveStatus
{
    public sealed class SetUserActiveStatusCommandValidator : AbstractValidator<SetUserActiveStatusCommand>
    {
        public SetUserActiveStatusCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(ApiResponseMessages.Validation.UserIdRequired);
            RuleFor(x => x.IsActive)
                .NotNull()
                .WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Active status"));
        }
    }
}
