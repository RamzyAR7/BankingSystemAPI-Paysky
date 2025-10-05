#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId
{
    public class GetAccountsByUserIdQueryValidator : AbstractValidator<GetAccountsByUserIdQuery>
    {
        public GetAccountsByUserIdQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "User id"));
        }
    }
}

