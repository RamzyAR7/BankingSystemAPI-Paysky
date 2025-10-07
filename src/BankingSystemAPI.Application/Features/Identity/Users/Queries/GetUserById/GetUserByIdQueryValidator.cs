#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserById
{
    /// <summary>
    /// Enhanced validator for GetUserByIdQuery with ResultExtensions patterns
    /// </summary>
    public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        private readonly IServiceProvider? _serviceProvider;

        public GetUserByIdQueryValidator(IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider;

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(ApiResponseMessages.Validation.UserIdRequired)
                .Must(BeValidUserId)
                .WithMessage(ApiResponseMessages.Validation.UserIdInvalidFormat);
        }

        private bool BeValidUserId(string userId)
        {
            // Basic format validation for User ID
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            // Log validation attempt if logger is available
            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetService<ILogger<GetUserByIdQueryValidator>>();

                var validationResult = userId.Length > 3 && userId.Length <= 450 // Reasonable ID length
                    ? Result.Success()
                    : Result.BadRequest(ApiResponseMessages.Validation.UserIdInvalidFormat);

                validationResult.OnSuccess(() =>
                {
                    logger?.LogDebug("[VALIDATION] User ID format validation passed: {UserId}", userId);
                })
                .OnFailure(errors =>
                {
                    logger?.LogWarning("[VALIDATION] User ID format validation failed: {UserId}, Errors={Errors}",
                        userId, string.Join("|", errors));
                });

                return validationResult.IsSuccess;
            }

            return userId.Length > 3 && userId.Length <= 450;
        }
    }
}
