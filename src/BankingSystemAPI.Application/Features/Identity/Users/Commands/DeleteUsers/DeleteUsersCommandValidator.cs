using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers
{
    public sealed class DeleteUsersCommandValidator : AbstractValidator<DeleteUsersCommand>
    {
        public DeleteUsersCommandValidator()
        {
            RuleFor(x => x.UserIds)
                .NotNull()
                .WithMessage("User IDs collection cannot be null.")
                .Must(ids => ids.Any())
                .WithMessage("At least one user ID must be provided.")
                .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage("All user IDs must be valid (non-empty).")
                .Must(ids => ids.Distinct().Count() == ids.Count())
                .WithMessage("Duplicate user IDs are not allowed.");
        }
    }
}