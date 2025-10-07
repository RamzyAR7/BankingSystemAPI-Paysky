#region Usings
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
using BankingSystemAPI.Domain.Constant;
using System.Linq;
#endregion


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
            var validationResult = ValidateStringInput(username, "Username");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));

            result.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.UserRetrievedByUsername, username))
                  .OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByUsernameFailed, username, string.Join(", ", errors)));

            return result;
        }

        public async Task<Result<UserResDto>> GetUserByIdAsync(string userId)
        {
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));

            result.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.UserRetrievedById, userId))
                  .OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, userId, string.Join(", ", errors)));

            return result;
        }

        public async Task<Result<UserResDto>> GetUserByEmailAsync(string email)
        {
            var validationResult = ValidateStringInput(email, "Email");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var user = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));

            result.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.UserRetrievedByEmail, email))
                  .OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByEmailFailed, email, string.Join(", ", errors)));

            return result;
        }

        #endregion

        #region User Bank and Role Methods

        public async Task<Result<string?>> GetUserRoleAsync(string userId)
        {
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<string?>.Failure(validationResult.ErrorItems);

            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                var result = Result<string?>.Failure(new ResultError(ErrorType.NotFound, ApiResponseMessages.Validation.UserNotFound));
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRoleRetrieveFailed, userId, string.Join(", ", errors)));
                return result;
            }

            string? roleName = null;
            if (user.Role != null && !string.IsNullOrEmpty(user.Role.Name))
            {
                roleName = user.Role.Name;
            }
            else
            {
                var roles = await _userManager.GetRolesAsync(user);
                roleName = roles.FirstOrDefault();
            }

            var successResult = Result<string?>.Success(roleName);
            successResult.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.UserRoleRetrieved, userId));
            return successResult;
        }

        public async Task<Result<IList<UserResDto>>> GetUsersByBankIdAsync(int bankId)
        {
            var users = await _userManager.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .Where(u => u.BankId == bankId)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserResDto>>(users);
            var result = Result<IList<UserResDto>>.Success(userDtos);

            result.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.UsersRetrievedForBankId, users.Count, bankId));
            return result;
        }

        public async Task<Result<bool>> IsBankActiveAsync(int bankId)
        {
            if (bankId <= 0)
            {
                var validationResult = Result<bool>.BadRequest(ApiResponseMessages.Validation.BankIdGreaterThanZero);
                validationResult.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, bankId, "Invalid BankId"));
                return validationResult;
            }

            var contextBank = await _userManager.Users
                .Where(u => u.BankId == bankId)
                .Select(u => u.Bank)
                .FirstOrDefaultAsync();

            var isActive = contextBank?.IsActive ?? false;
            var result = Result<bool>.Success(isActive);
            result.OnSuccess(() => _logger.LogDebug(ApiResponseMessages.Logging.BankActiveStatusChecked, bankId, isActive));
            return result;
        }

        #endregion

        #region User Management Operations

        public async Task<Result<UserResDto>> CreateUserAsync(UserReqDto user)
        {
            var existingUser = await _userManager.FindByNameAsync(user.Username);
            if (existingUser != null)
            {
                var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UsernameAlreadyExists);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailedUsernameExists, user.Username));
                return result;
            }

            existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser != null)
            {
                var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.EmailAlreadyExists);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailedEmailExists, user.Email));
                return result;
            }

            var entity = _mapper.Map<ApplicationUser>(user);

            entity.BankId = user.BankId;
            entity.IsActive = true;

            ApplicationRole? targetRole = null;
            if (!string.IsNullOrEmpty(user.Role))
            {
                targetRole = await _roleManager.FindByNameAsync(user.Role);
                if (targetRole == null)
                {
                    var result = Result<UserResDto>.BadRequest(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, "Role", user.Role));
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailedRoleNotFound, user.Role));
                    return result;
                }

                entity.RoleId = targetRole.Id;
            }
            else
            {
                entity.RoleId = string.Empty;
            }

            var identityResult = await _userManager.CreateAsync(entity, user.Password!);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errs)));
                return result;
            }

            if (targetRole != null && !string.IsNullOrEmpty(targetRole.Name))
            {
                var roleResult = await _userManager.AddToRoleAsync(entity, targetRole.Name);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(entity);
                    var errors = roleResult.Errors.Select(e => e.Description);
                    var result = Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                    result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errs)));
                    return result;
                }
            }

            var createdUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == entity.Id);

            var userDto = _mapper.Map<UserResDto>(createdUser ?? entity);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.UserCreated, user.Username));
            return successResult;
        }

        public async Task<Result<UserResDto>> UpdateUserAsync(string userId, UserEditDto user)
        {
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, userId, string.Join(", ", errors)));
                return result;
            }

            _mapper.Map(user, existingUser);

            var identityResult = await _userManager.UpdateAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.UserUpdateFailed, userId, string.Join(", ", errs)));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.UserUpdated, userId));
            return successResult;
        }

        public async Task<Result<UserResDto>> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var existingUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, userId, string.Join(", ", errors)));
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
                var result = Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.PasswordChangeFailed, userId, string.Join(", ", errs)));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser);
            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.PasswordChanged, userId));
            return successResult;
        }

        public async Task<Result<UserResDto>> DeleteUserAsync(string userId)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            var existingUser = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
            {
                var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, userId, string.Join(", ", errors)));
                return result;
            }

            var userDto = _mapper.Map<UserResDto>(existingUser); // Map before deletion

            var identityResult = await _userManager.DeleteAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.UserDeletionFailed, userId, string.Join(", ", errs)));
                return result;
            }

            var successResult = Result<UserResDto>.Success(userDto);
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.UserDeleted, userId));
            return successResult;
        }

        public async Task<Result<bool>> DeleteRangeOfUsersAsync(IEnumerable<string> userIds)
        {
            var userIdsList = userIds.ToList();
            if (!userIdsList.Any())
            {
                var result = Result<bool>.BadRequest(ApiResponseMessages.Validation.NoUserIdsProvided);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.BulkUserDeletionFailed, "NoIds", string.Join(", ", errors)));
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
                        var result = Result<bool>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                        result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.BulkUserDeletionFailed, userId, string.Join(", ", errs)));
                        return result;
                    }
                }
            }

            var successResult = Result<bool>.Success(true);
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.BulkUserDeletionCompleted, userIdsList.Count));
            return successResult;
        }

        public async Task<Result> SetUserActiveStatusAsync(string userId, bool isActive)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result.Failure(validationResult.ErrorItems);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                var result = Result.BadRequest(ApiResponseMessages.Validation.UserNotFound);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserRetrieveByIdFailed, userId, string.Join(", ", errors)));
                return result;
            }

            // Prevent deactivation of SuperAdmin users
            var roles = await _userManager.GetRolesAsync(user);
            if (roles != null && roles.Any(r => string.Equals(r, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase)) && !isActive)
            {
                var result = Result.Failure(new ResultError(ErrorType.Validation, ApiResponseMessages.Validation.SuperAdminCannotBeDeactivated));
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.SetUserActiveStatusFailed, userId, string.Join(", ", errors)));
                return result;
            }

            user.IsActive = isActive;
            var identityResult = await _userManager.UpdateAsync(user);

            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                var result = Result.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                result.OnFailure(errs => _logger.LogError(ApiResponseMessages.Logging.SetUserActiveStatusFailed, userId, string.Join(", ", errs)));
                return result;
            }

            var successResult = Result.Success();
            successResult.OnSuccess(() => _logger.LogInformation(ApiResponseMessages.Logging.SetUserActiveStatusChanged, isActive, userId));
            return successResult;
        }

        #endregion

        #region Private Helper Methods

        private Result ValidateStringInput(string input, string fieldName)
        {
            return string.IsNullOrWhiteSpace(input)
                ? Result.BadRequest(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, fieldName))
                : Result.Success();
        }

        #endregion
    }
}

