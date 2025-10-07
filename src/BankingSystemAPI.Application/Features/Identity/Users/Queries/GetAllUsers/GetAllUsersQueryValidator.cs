#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers
{
    public sealed class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
    {
        public GetAllUsersQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage(ApiResponseMessages.Validation.PageNumberAndPageSizeGreaterThanZero);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage(ApiResponseMessages.Validation.PageNumberAndPageSizeGreaterThanZero)
                .LessThanOrEqualTo(100)
                .WithMessage(string.Format(ApiResponseMessages.Validation.FieldLengthMaxFormat, "Page size", 100));

            RuleFor(x => x.OrderDirection)
                .Must(direction => string.IsNullOrEmpty(direction) ||
                      direction.Equals("ASC", StringComparison.OrdinalIgnoreCase) ||
                      direction.Equals("DESC", StringComparison.OrdinalIgnoreCase))
                .WithMessage(ApiResponseMessages.Validation.OrderDirectionMustBeAscOrDesc);
        }
    }
}
