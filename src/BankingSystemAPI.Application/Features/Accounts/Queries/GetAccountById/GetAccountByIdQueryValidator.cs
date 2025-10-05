#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById
{
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        public GetAccountByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(ApiResponseMessages.Validation.InvalidIdFormat.Replace("{0}", "Account id"));
        }
    }
}

