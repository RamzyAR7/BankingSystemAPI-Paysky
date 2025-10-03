using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class UserRolesService : IUserRolesService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserRolesService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<UserRoleUpdateResultDto>> UpdateUserRolesAsync(UpdateUserRolesDto dto)
        {
            // Validate input
            var inputValidation = ValidateInput(dto);
            if (inputValidation.IsFailure)
                return Result<UserRoleUpdateResultDto>.Failure(inputValidation.Errors);

            // Get user
            var userResult = await GetUserAsync(dto.UserId);
            if (userResult.IsFailure)
                return Result<UserRoleUpdateResultDto>.Failure(userResult.Errors);

            var user = userResult.Value!;

            // Process based on role assignment
            return string.IsNullOrEmpty(dto.Role) 
                ? await RemoveAllRolesAsync(user)
                : await UpdateUserRoleAsync(user, dto.Role);
        }

        private Result ValidateInput(UpdateUserRolesDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                return Result.BadRequest("User ID cannot be null or empty.");
            
            return Result.Success();
        }

        private async Task<Result<ApplicationUser>> GetUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user.ToResult($"User with ID '{userId}' not found.");
        }

        private async Task<Result<UserRoleUpdateResultDto>> RemoveAllRolesAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Remove existing roles if any
            if (userRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => e.Description);
                    return Result<UserRoleUpdateResultDto>.Failure(errors);
                }
            }

            // Clear FK on user
            user.RoleId = string.Empty;
            var updateResult = await _userManager.UpdateAsync(user);
            
            if (updateResult.Succeeded)
            {
                var successResult = CreateSuccessResult(user, null);
                return Result<UserRoleUpdateResultDto>.Success(successResult);
            }
            else
            {
                var errors = updateResult.Errors.Select(e => e.Description);
                return Result<UserRoleUpdateResultDto>.Failure(errors);
            }
        }

        private async Task<Result<UserRoleUpdateResultDto>> UpdateUserRoleAsync(ApplicationUser user, string roleName)
        {
            var targetRoleName = roleName.Trim();

            // Validate target role exists
            var targetRole = await _roleManager.FindByNameAsync(targetRoleName);
            if (targetRole == null)
                return Result<UserRoleUpdateResultDto>.BadRequest($"Target role '{targetRoleName}' does not exist.");

            // Remove existing roles
            var removeResult = await RemoveExistingRolesAsync(user);
            if (removeResult.IsFailure)
                return Result<UserRoleUpdateResultDto>.Failure(removeResult.Errors);

            // Add new role
            var addResult = await AddNewRoleAsync(user, targetRoleName);
            if (addResult.IsFailure)
                return Result<UserRoleUpdateResultDto>.Failure(addResult.Errors);

            // Update user role FK
            var updateResult = await UpdateUserRoleForeignKeyAsync(user, targetRole);
            if (updateResult.IsFailure)
                return Result<UserRoleUpdateResultDto>.Failure(updateResult.Errors);

            // Return success result
            var successResult = CreateSuccessResult(user, targetRole.Name);
            return Result<UserRoleUpdateResultDto>.Success(successResult);
        }

        private async Task<Result> RemoveExistingRolesAsync(ApplicationUser user)
        {
            var existingUserRoles = await _userManager.GetRolesAsync(user);
            if (!existingUserRoles.Any())
                return Result.Success();

            var removeResult = await _userManager.RemoveFromRolesAsync(user, existingUserRoles);
            return removeResult.Succeeded
                ? Result.Success()
                : Result.Failure(removeResult.Errors.Select(e => e.Description));
        }

        private async Task<Result> AddNewRoleAsync(ApplicationUser user, string roleName)
        {
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            return addResult.Succeeded
                ? Result.Success()
                : Result.Failure(addResult.Errors.Select(e => e.Description));
        }

        private async Task<Result> UpdateUserRoleForeignKeyAsync(ApplicationUser user, ApplicationRole targetRole)
        {
            user.RoleId = targetRole.Id;
            var finalUpdateResult = await _userManager.UpdateAsync(user);
            return finalUpdateResult.Succeeded
                ? Result.Success()
                : Result.Failure(finalUpdateResult.Errors.Select(e => e.Description));
        }

        private UserRoleUpdateResultDto CreateSuccessResult(ApplicationUser user, string? roleName)
        {
            return new UserRoleUpdateResultDto
            {
                UserId = user.Id,
                PreviousRole = null, // This would need to be tracked if we want to show previous role
                NewRole = roleName,
                UserRole = new UserRoleResDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roleName
                }
            };
        }
    }
}