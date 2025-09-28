using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Application.Interfaces.Authorization;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class UserRolesService : IUserRolesService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ICurrentUserService _currentUserService;

        public UserRolesService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _currentUserService = currentUserService;
        }
        public async Task<UserRoleUpdateResultDto> UpdateUserRolesAsync(UpdateUserRolesDto dto)
        {
            var result = new UserRoleUpdateResultDto();

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                result.Errors.Add(new IdentityError { Description = $"User with ID '{dto.UserId}' not found." });
                result.Succeeded = false;
                return result;
            }

            // If Role is explicitly null -> remove all roles from user
            if (dto.Role == null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
                    if (!removeResult.Succeeded)
                    {
                        result.Errors.AddRange(removeResult.Errors);
                        result.Succeeded = false;
                        return result;
                    }
                }

                // Clear FK on user and persist (use RoleId empty string to satisfy required FK)
                user.RoleId = string.Empty;
                await _userManager.UpdateAsync(user);

                result.UserRole = new UserRoleResDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = null
                };

                result.Succeeded = true;
                return result;
            }

            // Do not allow empty/whitespace role in request
            if (string.IsNullOrWhiteSpace(dto.Role))
            {
                result.Errors.Add(new IdentityError { Description = "Role is required." });
                result.Succeeded = false;
                return result;
            }

            // Previous logic when Role is provided
            var targetRoleName = dto.Role.Trim();

            // Get acting user's role from store (single-role invariant)
            var currentUserRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = await _currentUserService.IsInRoleAsync(UserRole.SuperAdmin.ToString());

            // Validate target role exists
            var targetRole = await _roleManager.FindByNameAsync(targetRoleName);
            if (targetRole == null)
            {
                result.Errors.Add(new IdentityError { Description = $"Target role '{targetRoleName}' does not exist." });
                result.Succeeded = false;
                return result;
            }

            // Prevent non-superadmin from assigning SuperAdmin
            if (!isSuperAdmin && string.Equals(targetRoleName, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new IdentityError { Description = "Not authorized to assign SuperAdmin role." });
                result.Succeeded = false;
                return result;
            }

            // Remove all existing roles then add the single target role (ensure single-role invariant)
            var existingUserRoles = await _userManager.GetRolesAsync(user);
            if (existingUserRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, existingUserRoles);
                if (!removeResult.Succeeded)
                {
                    result.Errors.AddRange(removeResult.Errors);
                    result.Succeeded = false;
                    return result;
                }
            }
            // Add only the new role
            var addResult = await _userManager.AddToRoleAsync(user, targetRoleName);
            if (!addResult.Succeeded)
            {
                result.Errors.AddRange(addResult.Errors);
                result.Succeeded = false;
                return result;
            }
            // Set FK on user and persist
            user.RoleId = targetRole.Id;
            await _userManager.UpdateAsync(user);

            var role = await _roleManager.FindByIdAsync(user.RoleId);
            result.UserRole = new UserRoleResDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = role.Name    
            };
            result.Succeeded = true;
            return result;
        }
    }
}