using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.SetUserActiveStatus
{
    public sealed class SetUserActiveStatusCommandValidator : AbstractValidator<SetUserActiveStatusCommand>
    {
        public SetUserActiveStatusCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
            RuleFor(x => x.IsActive)
                .NotNull()
                .WithMessage("Active status must be specified.");
        }
    }
}