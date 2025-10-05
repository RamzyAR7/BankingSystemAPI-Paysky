#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserByUsername
{
    public sealed class GetUserByUsernameQueryValidator : AbstractValidator<GetUserByUsernameQuery>
    {
        public GetUserByUsernameQueryValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Username"));
        }
    }
}
