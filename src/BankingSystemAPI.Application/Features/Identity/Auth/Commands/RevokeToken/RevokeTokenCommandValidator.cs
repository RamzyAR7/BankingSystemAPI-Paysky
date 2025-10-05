#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RevokeToken
{
    public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
    {
        public RevokeTokenCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "User ID"));
        }
    }
}
