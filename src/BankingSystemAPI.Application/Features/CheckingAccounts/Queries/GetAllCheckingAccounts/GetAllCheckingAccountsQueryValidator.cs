#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts
{
    public class GetAllCheckingAccountsQueryValidator : AbstractValidator<GetAllCheckingAccountsQuery>
    {
        public GetAllCheckingAccountsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ApiResponseMessages.Validation.PageNumberAndPageSizeGreaterThanZero);
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage(ApiResponseMessages.Validation.PageNumberAndPageSizeGreaterThanZero);
        }
    }
}

