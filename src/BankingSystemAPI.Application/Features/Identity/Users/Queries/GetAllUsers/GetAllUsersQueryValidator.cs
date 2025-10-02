using FluentValidation;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetAllUsers
{
    public sealed class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
    {
        public GetAllUsersQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100.");

            RuleFor(x => x.OrderDirection)
                .Must(direction => string.IsNullOrEmpty(direction) || 
                      direction.Equals("ASC", StringComparison.OrdinalIgnoreCase) || 
                      direction.Equals("DESC", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Order direction must be 'ASC' or 'DESC'.");
        }
    }
}