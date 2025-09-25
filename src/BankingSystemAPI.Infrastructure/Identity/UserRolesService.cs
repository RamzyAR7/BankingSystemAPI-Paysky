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

            // If Role is null -> remove all roles from the user
            if (dto.Role == null)
            {
                var existingRoles = await _userManager.GetRolesAsync(user);
                if (!existingRoles.Any())
                {
                    result.Succeeded = true;
                    result.UserRole = new UserRoleResDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        Role = null
                    };
                    return result;
                }

                var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!removeResult.Succeeded)
                {
                    result.Errors.AddRange(removeResult.Errors);
                    result.Succeeded = false;
                    return result;
                }

                result.Succeeded = true;
                result.UserRole = new UserRoleResDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = null
                };
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
            // Add only the new role
            var addResult = await _userManager.AddToRoleAsync(user, targetRoleName);
            if (!addResult.Succeeded)
            {
                result.Errors.AddRange(addResult.Errors);
                result.Succeeded = false;
                return result;
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);
            var role = updatedRoles.ToList();

            result.UserRole = new UserRoleResDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = role.FirstOrDefault()    
            };
            result.Succeeded = true;
            return result;
        }
    }
}