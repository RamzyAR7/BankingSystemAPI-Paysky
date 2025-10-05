#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers
{
    public sealed class DeleteUsersCommandValidator : AbstractValidator<DeleteUsersCommand>
    {
        public DeleteUsersCommandValidator()
        {
            RuleFor(x => x.UserIds)
                .NotNull()
                .WithMessage(ApiResponseMessages.Validation.UserIdsCollectionCannotBeNull)
                .Must(ids => ids.Any())
                .WithMessage(ApiResponseMessages.Validation.AtLeastOneUserIdProvided)
                .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage(ApiResponseMessages.Validation.AllUserIdsMustBeValid)
                .Must(ids => ids.Distinct().Count() == ids.Count())
                .WithMessage(ApiResponseMessages.Validation.DuplicateUserIdsNotAllowed);
        }
    }
}
