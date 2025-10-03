using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser
{
    public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserService userService,
            ILogger<UpdateUserCommandHandler> logger,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            // Chain authorization, validation, and update using ResultExtensions
            var authResult = await ValidateAuthorizationAsync(request.UserId);
            if (authResult.IsFailure)
                return Result<UserResDto>.Failure(authResult.Errors);

            var userResult = await ValidateUserExistsAsync(request.UserId);
            if (userResult.IsFailure)
                return Result<UserResDto>.Failure(userResult.Errors);

            var uniquenessResult = await ValidateUniquenessAsync(request, userResult.Value!);
            if (uniquenessResult.IsFailure)
                return Result<UserResDto>.Failure(uniquenessResult.Errors);

            var updateResult = await ExecuteUpdateAsync(request);
            
            // Add side effects using ResultExtensions
            updateResult.OnSuccess(() => 
                {
                    _logger.LogInformation("User updated successfully: {UserId}, Username: {Username}", 
                        request.UserId, request.UserEdit.Username);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("User update failed for: {UserId}. Errors: {Errors}",
                        request.UserId, string.Join(", ", errors));
                });

            return updateResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(string userId)
        {
            if (_userAuthorizationService == null)
                return Result.Success();

            try
            {
                await _userAuthorizationService.CanModifyUserAsync(userId, UserModificationOperation.Edit);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private async Task<Result<UserResDto>> ValidateUserExistsAsync(string userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            return result.Succeeded
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }

        private async Task<Result<UpdateValidationContext>> ValidateUniquenessAsync(UpdateUserCommand request, UserResDto existingUser)
        {
            // Check for duplicates within the same bank using functional approach
            var usersInBankResult = await GetUsersInSameBankAsync(existingUser.BankId ?? 0);
            if (usersInBankResult.IsFailure)
                return Result<UpdateValidationContext>.Failure(usersInBankResult.Errors);

            var usersInSameBank = usersInBankResult.Value!;

            // Efficient conflict detection using functional patterns
            var conflicts = CheckForConflicts(request, usersInSameBank);
            
            return conflicts.Any()
                ? Result<UpdateValidationContext>.BadRequest($"Another user with conflicting {string.Join(", ", conflicts)} already exists in this bank.")
                : Result<UpdateValidationContext>.Success(new UpdateValidationContext 
                { 
                    ExistingUser = existingUser,
                    UsersInBank = usersInSameBank 
                });
        }

        private async Task<Result<IList<UserResDto>>> GetUsersInSameBankAsync(int bankId)
        {
            var result = await _userService.GetUsersByBankIdAsync(bankId);
            return result.Succeeded
                ? Result<IList<UserResDto>>.Success(result.Value!)
                : Result<IList<UserResDto>>.Failure(result.Errors);
        }

        private List<string> CheckForConflicts(UpdateUserCommand request, IList<UserResDto> usersInBank)
        {
            var conflicts = new List<string>();
            
            var conflictingUser = usersInBank.FirstOrDefault(u =>
                u.Id != request.UserId &&
                (string.Equals(u.Username, request.UserEdit.Username, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.Email, request.UserEdit.Email, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.NationalId, request.UserEdit.NationalId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.PhoneNumber, request.UserEdit.PhoneNumber, StringComparison.OrdinalIgnoreCase)));

            if (conflictingUser != null)
            {
                if (string.Equals(conflictingUser.Username, request.UserEdit.Username, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add("username");
                if (string.Equals(conflictingUser.Email, request.UserEdit.Email, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add("email");
                if (string.Equals(conflictingUser.NationalId, request.UserEdit.NationalId, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add("national ID");
                if (string.Equals(conflictingUser.PhoneNumber, request.UserEdit.PhoneNumber, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add("phone number");
            }

            return conflicts;
        }

        private async Task<Result<UserResDto>> ExecuteUpdateAsync(UpdateUserCommand request)
        {
            var result = await _userService.UpdateUserAsync(request.UserId, request.UserEdit);
            return result.Succeeded
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }

        private class UpdateValidationContext
        {
            public UserResDto ExistingUser { get; set; } = null!;
            public IList<UserResDto> UsersInBank { get; set; } = null!;
        }
    }
}