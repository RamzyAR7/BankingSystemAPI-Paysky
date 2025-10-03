using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class UserService : IUserService
    {
        #region Fields and Constructor

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager, 
            RoleManager<ApplicationRole> roleManager,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region User Retrieval Methods

        public async Task<Result<UserResDto>> GetUserByUsernameAsync(string username)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(username, "Username");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            var result = user.ToResult("User not found.")
                .Map(u => _mapper.Map<UserResDto>(u));

            // Add side effects using ResultExtensions
            result.OnSuccess(() => _logger.LogDebug("User retrieved successfully by username: {Username}", username))
                  .OnFailure(errors => _logger.LogWarning("Failed to retrieve user by username: {Username}, Errors: {Errors}", 
                      username, string.Join(", ", errors)));

            return result;
        }

        public async Task<Result<UserResDto>> GetUserByIdAsync(string userId)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var result = user.ToResult("User not found.")
                .Map(u => _mapper.Map<UserResDto>(u));

            // Add side effects using ResultExtensions
            result.OnSuccess(() => _logger.LogDebug("User retrieved successfully by ID: {UserId}", userId))
                  .OnFailure(errors => _logger.LogWarning("Failed to retrieve user by ID: {UserId}, Errors: {Errors}", 
                      userId, string.Join(", ", errors)));

            return result;
        }

        public async Task<Result<UserResDto>> GetUserByEmailAsync(string email)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(email, "Email");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            var result = user.ToResult("User not found.")
                .Map(u => _mapper.Map<UserResDto>(u));

            // Add side effects using ResultExtensions
            result.OnSuccess(() => _logger.LogDebug("User retrieved successfully by email: {Email}", email))
                  .OnFailure(errors => _logger.LogWarning("Failed to retrieve user by email: {Email}, Errors: {Errors}", 
                      email, string.Join(", ", errors)));

            return result;
        }

        #endregion

        #region User Bank and Role Methods

        public async Task<Result<string?>> GetUserRoleAsync(string userId)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<string?>.Failure(validationResult.Errors);

            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                var result = Result<string?>.Failure("User not found.");
                result.OnFailure(errors => _logger.LogWarning("Failed to retrieve user role for ID: {UserId}, Errors: {Errors}", 
                    userId, string.Join(", ", errors)));
                return result;
            }

            // First try to get role from the Role navigation property
            string? roleName = null;
            if (user.Role != null && !string.IsNullOrEmpty(user.Role.Name))
            {
                roleName = user.Role.Name;
            }
            else
            {
                // Fallback to ASP.NET Identity role system if navigation property is not available
                var roles = await _userManager.GetRolesAsync(user);
                roleName = roles.FirstOrDefault();
            }

            var successResult = Result<string?>.Success(roleName);
            successResult.OnSuccess(() => _logger.LogDebug("User role retrieved successfully for ID: {UserId}", userId));
            return successResult;
        }

        public async Task<Result<IList<UserResDto>>> GetUsersByBankIdAsync(int bankId)
        {
            if (bankId <= 0)
            {
                var validationResult = Result<IList<UserResDto>>.BadRequest("Bank ID must be greater than zero.");
                validationResult.OnFailure(errors => _logger.LogWarning("Invalid bank ID for user retrieval: {BankId}", bankId));
                return validationResult;
            }

            var users = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .Where(u => u.BankId == bankId)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserResDto>>(users);
            var result = Result<IList<UserResDto>>.Success(userDtos);
            
            // Add side effects using ResultExtensions
            result.OnSuccess(() => _logger.LogDebug("Retrieved {Count} users for bank ID: {BankId}", users.Count, bankId));
            return result;
        }

        public async Task<Result<IList<UserResDto>>> GetUsersByBankNameAsync(string bankName)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(bankName, "Bank name");
            if (validationResult.IsFailure)
                return Result<IList<UserResDto>>.Failure(validationResult.Errors);

            var users = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .Where(u => u.Bank != null && u.Bank.Name == bankName)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserResDto>>(users);
            var result = Result<IList<UserResDto>>.Success(userDtos);
            
            // Add side effects using ResultExtensions
            result.OnSuccess(() => _logger.LogDebug("Retrieved {Count} users for bank name: {BankName}", users.Count, bankName));
            return result;
        }

        public async Task<Result<bool>> IsBankActiveAsync(int bankId)
        {
            if (bankId <= 0)
            {
                var validationResult = Result<bool>.BadRequest("Bank ID must be greater than zero.");
                validationResult.OnFailure(errors => _logger.LogWarning("Invalid bank ID for active status check: {BankId}", bankId));
                return validationResult;
            }

            var contextBank = await _userManager.Users
                .Where(u => u.BankId == bankId)
                .Select(u => u.Bank)
                .FirstOrDefaultAsync();
            
            var isActive = contextBank?.IsActive ?? false;
            var result = Result<bool>.Success(isActive);
            result.OnSuccess(() => _logger.LogDebug("Bank active status checked for ID: {BankId}, IsActive: {IsActive}", bankId, isActive));
            return result;
        }

        #endregion

        #region User Management Operations

        public async Task<Result<UserResDto>> CreateUserAsync(UserReqDto user)
        {
            // Check for existing user first - return failure results instead of throwing exceptions
            var existingUser = await _userManager.FindByNameAsync(user.Username);
            if (existingUser != null)
            {
                var result = Result<UserResDto>.BadRequest("Username already exists.");
                result.OnFailure(errors => _logger.LogWarning("User creation failed - username exists: {Username}", user.Username));
                return result;
            }

            existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser != null)
            {
                var result = Result<UserResDto>.BadRequest("Email already exists.");
                result.OnFailure(errors => _logger.LogWarning("User creation failed - email exists: {Email}", user.Email));
                return result;
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
                    var result = Result<UserResDto>.BadRequest($"Role '{user.Role}' does not exist.");
                    result.OnFailure(errors => _logger.LogWarning("User creation failed - role not found: {Role}", user.Role));
                    return result;
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
                var result = Result<UserResDto>.Failure(errors);
                result.OnFailure(errs => _logger.LogError("User creation failed for {Username}: {Errors}", user.Username, string.Join(", ", errs)));
                return result;
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
                    var result = Result<UserResDto>.Failure(errors);
                    result.OnFailure(errs => _logger.LogError("User role assignment failed for {Username}: {Errors}", user.Username, string.Join(", ", errs)));
                    return result;
                }
            }

            // Get created user with relations for mapping
            var createdUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == entity.Id);

            // Map result using AutoMapper
            var userDto = _mapper.Map<UserResDto>(createdUser ?? entity);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation("User created successfully: {Username}", user.Username));
            return successResult;
        }

        public async Task<Result<UserResDto>> UpdateUserAsync(string userId, UserEditDto user)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest("User not found.");
                result.OnFailure(errors => _logger.LogWarning("User update failed - user not found: {UserId}", userId));
                return result;
            }

            // Map changes
            _mapper.Map(user, existingUser);

            var identityResult = await _userManager.UpdateAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result<UserResDto>.Failure(errors);
                result.OnFailure(errs => _logger.LogError("User update failed for {UserId}: {Errors}", userId, string.Join(", ", errs)));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation("User updated successfully: {UserId}", userId));
            return successResult;
        }

        public async Task<Result<UserResDto>> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var existingUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest("User not found.");
                result.OnFailure(errors => _logger.LogWarning("Password change failed - user not found: {UserId}", userId));
                return result;
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
                var result = Result<UserResDto>.Failure(errors);
                result.OnFailure(errs => _logger.LogError("Password change failed for {UserId}: {Errors}", userId, string.Join(", ", errs)));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation("Password changed successfully for user: {UserId}", userId));
            return successResult;
        }

        public async Task<Result<UserResDto>> DeleteUserAsync(string userId)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.Errors);

            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest("User not found.");
                result.OnFailure(errors => _logger.LogWarning("User deletion failed - user not found: {UserId}", userId));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser); // Map before deletion

            var identityResult = await _userManager.DeleteAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result<UserResDto>.Failure(errors);
                result.OnFailure(errs => _logger.LogError("User deletion failed for {UserId}: {Errors}", userId, string.Join(", ", errs)));
                return result;
            }

            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation("User deleted successfully: {UserId}", userId));
            return successResult;
        }

        public async Task<Result<bool>> DeleteRangeOfUsersAsync(IEnumerable<string> userIds)
        {
            var userIdsList = userIds.ToList();
            if (!userIdsList.Any())
            {
                var result = Result<bool>.BadRequest("No user IDs provided.");
                result.OnFailure(errors => _logger.LogWarning("Bulk user deletion failed - no IDs provided"));
                return result;
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
                        var result = Result<bool>.Failure(errors);
                        result.OnFailure(errs => _logger.LogError("Bulk user deletion failed for {UserId}: {Errors}", userId, string.Join(", ", errs)));
                        return result;
                    }
                }
            }

            var successResult = Result<bool>.Success(true);
            successResult.OnSuccess(() => _logger.LogInformation("Bulk user deletion completed for {Count} users", userIdsList.Count));
            return successResult;
        }

        public async Task<Result> SetUserActiveStatusAsync(string userId, bool isActive)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result.Failure(validationResult.Errors);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                var result = Result.BadRequest("User not found.");
                result.OnFailure(errors => _logger.LogWarning("Set user active status failed - user not found: {UserId}", userId));
                return result;
            }

            user.IsActive = isActive;
            var identityResult = await _userManager.UpdateAsync(user);
            
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result.Failure(errors);
                result.OnFailure(errs => _logger.LogError("Set user active status failed for {UserId}: {Errors}", userId, string.Join(", ", errs)));
                return result;
            }

            var successResult = Result.Success();
            successResult.OnSuccess(() => _logger.LogInformation("User active status changed to {IsActive} for user: {UserId}", isActive, userId));
            return successResult;
        }

        #endregion

        #region Private Helper Methods

        private Result ValidateStringInput(string input, string fieldName)
        {
            return string.IsNullOrWhiteSpace(input)
                ? Result.BadRequest($"{fieldName} cannot be null or empty.")
                : Result.Success();
        }

        #endregion
    }
}
