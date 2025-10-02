using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class UserService : IUserService
    {
        #region Fields and Constructor

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;

        public UserService(
            UserManager<ApplicationUser> userManager, 
            RoleManager<ApplicationRole> roleManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        #endregion

        #region User Retrieval Methods

        public async Task<Result<UserResDto>> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Result<UserResDto>.Failure(new[] { "Username cannot be null or empty." });
            }

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            var userDto = _mapper.Map<UserResDto>(user);
            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<UserResDto>> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserResDto>.Failure(new[] { "User ID cannot be null or empty." });
            }

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            var userDto = _mapper.Map<UserResDto>(user);
            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<UserResDto>> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result<UserResDto>.Failure(new[] { "Email cannot be null or empty." });
            }

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            var userDto = _mapper.Map<UserResDto>(user);
            return Result<UserResDto>.Success(userDto);
        }

        #endregion

        #region User Bank and Role Methods

        public async Task<Result<string?>> GetUserRoleAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<string?>.Failure(new[] { "User ID cannot be null or empty." });
            }

            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return Result<string?>.Failure(new[] { "User not found." });
            }

            // First try to get role from the Role navigation property
            if (user.Role != null && !string.IsNullOrEmpty(user.Role.Name))
            {
                return Result<string?>.Success(user.Role.Name);
            }

            // Fallback to ASP.NET Identity role system if navigation property is not available
            var roles = await _userManager.GetRolesAsync(user);
            var firstRole = roles.FirstOrDefault();
            return Result<string?>.Success(firstRole);
        }

        public async Task<Result<IList<UserResDto>>> GetUsersByBankIdAsync(int bankId)
        {
            if (bankId <= 0)
            {
                return Result<IList<UserResDto>>.Failure(new[] { "Bank ID must be greater than zero." });
            }

            var users = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .Where(u => u.BankId == bankId)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserResDto>>(users);
            return Result<IList<UserResDto>>.Success(userDtos);
        }

        public async Task<Result<IList<UserResDto>>> GetUsersByBankNameAsync(string bankName)
        {
            if (string.IsNullOrWhiteSpace(bankName))
            {
                return Result<IList<UserResDto>>.Failure(new[] { "Bank name cannot be null or empty." });
            }

            var users = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .Where(u => u.Bank != null && u.Bank.Name == bankName)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserResDto>>(users);
            return Result<IList<UserResDto>>.Success(userDtos);
        }

        public async Task<Result<bool>> IsBankActiveAsync(int bankId)
        {
            if (bankId <= 0)
            {
                return Result<bool>.Failure(new[] { "Bank ID must be greater than zero." });
            }

            var contextBank = await _userManager.Users
                .Where(u => u.BankId == bankId)
                .Select(u => u.Bank)
                .FirstOrDefaultAsync();
            
            var isActive = contextBank?.IsActive ?? false;
            return Result<bool>.Success(isActive);
        }

        #endregion

        #region User Management Operations

        public async Task<Result<UserResDto>> CreateUserAsync(UserReqDto user)
        {
            // Check for existing user first - return failure results instead of throwing exceptions
            var existingUser = await _userManager.FindByNameAsync(user.Username);
            if (existingUser != null)
            {
                return Result<UserResDto>.Failure(new[] { "Username already exists." });
            }

            existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser != null)
            {
                return Result<UserResDto>.Failure(new[] { "Email already exists." });
            }

            // Map to entity using AutoMapper
            var entity = _mapper.Map<ApplicationUser>(user);
            
            // Set additional properties that might not be mapped
            entity.BankId = user.BankId;
            entity.IsActive = true;
            
            // Get and set role BEFORE creating user
            ApplicationRole? targetRole = null;
            if (!string.IsNullOrEmpty(user.Role))
            {
                targetRole = await _roleManager.FindByNameAsync(user.Role);
                if (targetRole == null)
                {
                    return Result<UserResDto>.Failure(new[] { $"Role '{user.Role}' does not exist." });
                }
                
                // Set the RoleId foreign key BEFORE creating the user
                entity.RoleId = targetRole.Id;
            }
            else
            {
                // If no role specified, set RoleId to empty string (satisfies required FK constraint)
                entity.RoleId = string.Empty;
            }

            // Create user with the RoleId properly set
            var identityResult = await _userManager.CreateAsync(entity, user.Password!);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<UserResDto>.Failure(errors);
            }

            // Add to AspNetUserRoles table for ASP.NET Identity consistency
            if (targetRole != null && !string.IsNullOrEmpty(targetRole.Name))
            {
                var roleResult = await _userManager.AddToRoleAsync(entity, targetRole.Name);
                if (!roleResult.Succeeded)
                {
                    // Cleanup: Delete the created user if role assignment fails
                    await _userManager.DeleteAsync(entity);
                    var errors = roleResult.Errors.Select(e => e.Description);
                    return Result<UserResDto>.Failure(errors);
                }
            }

            // Get created user with relations for mapping
            var createdUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == entity.Id);

            // Map result using AutoMapper
            var userDto = _mapper.Map<UserResDto>(createdUser ?? entity);
            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<UserResDto>> UpdateUserAsync(string userId, UserEditDto user)
        {
            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            // Map changes
            _mapper.Map(user, existingUser);

            var identityResult = await _userManager.UpdateAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<UserResDto>.Failure(errors);
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<UserResDto>> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto)
        {
            var existingUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            IdentityResult changeResult;

            // If current password is provided, use change password, otherwise reset
            if (!string.IsNullOrWhiteSpace(dto.CurrentPassword))
            {
                changeResult = await _userManager.ChangePasswordAsync(existingUser, dto.CurrentPassword, dto.NewPassword!);
            }
            else
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                changeResult = await _userManager.ResetPasswordAsync(existingUser, token, dto.NewPassword!);
            }

            if (!changeResult.Succeeded)
            {
                var errors = changeResult.Errors.Select(e => e.Description);
                return Result<UserResDto>.Failure(errors);
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<UserResDto>> DeleteUserAsync(string userId)
        {
            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                return Result<UserResDto>.Failure(new[] { "User not found." });
            }

            var userDto = _mapper.Map<UserResDto>(existingUser); // Map before deletion

            var identityResult = await _userManager.DeleteAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<UserResDto>.Failure(errors);
            }

            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<bool>> DeleteRangeOfUsersAsync(IEnumerable<string> userIds)
        {
            var userIdsList = userIds.ToList();
            if (!userIdsList.Any())
            {
                return Result<bool>.Failure(new[] { "No user IDs provided." });
            }

            foreach (var userId in userIdsList)
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                if (existingUser != null)
                {
                    var identityResult = await _userManager.DeleteAsync(existingUser);
                    if (!identityResult.Succeeded)
                    {
                        var errors = identityResult.Errors.Select(e => e.Description);
                        return Result<bool>.Failure(errors);
                    }
                }
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result> SetUserActiveStatusAsync(string userId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result.Failure(new[] { "User ID cannot be null or empty." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(new[] { "User not found." });
            }

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Result.Failure(errors);
            }

            return Result.Success();
        }

        #endregion
    }
}
