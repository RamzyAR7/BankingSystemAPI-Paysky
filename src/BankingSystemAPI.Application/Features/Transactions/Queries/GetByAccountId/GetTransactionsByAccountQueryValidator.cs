#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId
{
    public class GetTransactionsByAccountQueryValidator : AbstractValidator<GetTransactionsByAccountQuery>
    {
        public GetTransactionsByAccountQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0)
                .WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "AccountId"));

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMinFormat, "Page number", 1));

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Page size"));
        }
    }
}

