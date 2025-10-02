using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
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
            // Input validation
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
            {
                return Result<UserRoleUpdateResultDto>.Failure(new[] { "User ID cannot be null or empty." });
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return Result<UserRoleUpdateResultDto>.Failure(new[] { $"User with ID '{dto.UserId}' not found." });
            }

            // If Role is null or empty -> remove all roles from user
            if (string.IsNullOrEmpty(dto.Role))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
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
                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors.Select(e => e.Description);
                    return Result<UserRoleUpdateResultDto>.Failure(errors);
                }

                var result = new UserRoleUpdateResultDto
                {
                    UserRole = new UserRoleResDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        Role = null
                    },
                    Succeeded = true,
                    Errors = new List<IdentityError>()
                };

                return Result<UserRoleUpdateResultDto>.Success(result);
            }

            var targetRoleName = dto.Role.Trim();

            // Get target role
            var targetRole = await _roleManager.FindByNameAsync(targetRoleName);
            if (targetRole == null)
            {
                return Result<UserRoleUpdateResultDto>.Failure(new[] { $"Target role '{targetRoleName}' does not exist." });
            }

            // Remove all existing roles then add the single target role
            var existingUserRoles = await _userManager.GetRolesAsync(user);
            if (existingUserRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, existingUserRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => e.Description);
                    return Result<UserRoleUpdateResultDto>.Failure(errors);
                }
            }

            // Add the new role
            var addResult = await _userManager.AddToRoleAsync(user, targetRoleName);
            if (!addResult.Succeeded)
            {
                var errors = addResult.Errors.Select(e => e.Description);
                return Result<UserRoleUpdateResultDto>.Failure(errors);
            }

            // Set FK on user
            user.RoleId = targetRole.Id;
            var finalUpdateResult = await _userManager.UpdateAsync(user);
            if (!finalUpdateResult.Succeeded)
            {
                var errors = finalUpdateResult.Errors.Select(e => e.Description);
                return Result<UserRoleUpdateResultDto>.Failure(errors);
            }

            var successResult = new UserRoleUpdateResultDto
            {
                UserRole = new UserRoleResDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = targetRole.Name
                },
                Succeeded = true,
                Errors = new List<IdentityError>()
            };

            return Result<UserRoleUpdateResultDto>.Success(successResult);
        }
    }
}