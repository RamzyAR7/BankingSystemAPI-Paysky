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

            ApplicationUser? user;
            try
            {
                user = await _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == username);
            }
            catch (InvalidOperationException)
            {
                user = _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.UserName == username);
            }

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));
            return result;
        }

        public async Task<Result<UserResDto>> GetUserByIdAsync(string userId)
        {
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            ApplicationUser? user;
            try
            {
                user = await _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (InvalidOperationException)
            {
                user = _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Id == userId);
            }

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));
            return result;
        }

        public async Task<Result<UserResDto>> GetUserByEmailAsync(string email)
        {
            var validationResult = ValidateStringInput(email, "Email");
            if (validationResult.IsFailure)
                return Result<UserResDto>.Failure(validationResult.ErrorItems);

            ApplicationUser? user;
            try
            {
                user = await _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (InvalidOperationException)
            {
                user = _userManager.Users
                    .Include(u => u.Accounts).ThenInclude(a => a.Currency)
                    .Include(u => u.Bank)
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Email == email);
            }

            var result = user.ToResult(ApiResponseMessages.Validation.UserNotFound)
                .Map(u => _mapper.Map<UserResDto>(u));
            return result;
        }

        #endregion

        #region User Bank and Role Methods

        public async Task<Result<string?>> GetUserRoleAsync(string userId)
        {
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result<string?>.Failure(validationResult.ErrorItems);

            ApplicationUser? user;
            try
            {
                user = await _userManager.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (InvalidOperationException)
            {
                user = _userManager.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Id == userId);
            }

            if (user == null)
            {
                return Result<string?>.Failure(new ResultError(ErrorType.NotFound, ApiResponseMessages.Validation.UserNotFound));
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

            return Result<string?>.Success(roleName);
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
            return Result<IList<UserResDto>>.Success(userDtos);
        }

        public async Task<Result<bool>> IsBankActiveAsync(int bankId)
        {
            if (bankId <= 0)
            {
                return Result<bool>.BadRequest(ApiResponseMessages.Validation.BankIdGreaterThanZero);
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
            // Normalize inputs (trim) and prepare lowercase variants for comparisons
            var normalizedUsername = user.Username?.Trim();
            var normalizedEmail = user.Email?.Trim();
            var normalizedNationalId = string.IsNullOrWhiteSpace(user.NationalId) ? null : user.NationalId.Trim();
            var normalizedPhone = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber.Trim();
            var normalizedFullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim();

            var usernameLower = normalizedUsername?.ToLowerInvariant();
            var emailLower = normalizedEmail?.ToLowerInvariant();
            var nationalIdLower = normalizedNationalId?.ToLowerInvariant();
            var phoneLower = normalizedPhone?.ToLowerInvariant();
            var fullNameLower = normalizedFullName?.ToLowerInvariant();

            // Single-query conflict check scoped by BankId to reduce DB round-trips.
            var conflictsQuery = _userManager.Users
                .Where(u => u.BankId == user.BankId && (
                    u.UserName.ToLower() == usernameLower ||
                    u.Email.ToLower() == emailLower ||
                    (nationalIdLower != null && u.NationalId.ToLower() == nationalIdLower) ||
                    (phoneLower != null && u.PhoneNumber.ToLower() == phoneLower) ||
                    (fullNameLower != null && u.FullName.ToLower() == fullNameLower)
                ));

            List<ApplicationUser> conflicts;
            try
            {
                conflicts = await conflictsQuery.ToListAsync();
            }
            catch (InvalidOperationException)
            {
                // In unit tests the IQueryable may not support async. Fall back to synchronous enumeration.
                conflicts = conflictsQuery.ToList();
            }

            if (conflicts.Any())
            {
                if (conflicts.Any(u => u.UserName == user.Username))
                {
                    var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UsernameAlreadyExists);
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailedUsernameExists, user.Username));
                    return result;
                }

                if (conflicts.Any(u => u.Email == user.Email))
                {
                    var result = Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.EmailAlreadyExists);
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailedEmailExists, user.Email));
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(user.NationalId) && conflicts.Any(u => u.NationalId == user.NationalId))
                {
                    var result = Result<UserResDto>.BadRequest(string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "National ID"));
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errors)));
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && conflicts.Any(u => u.PhoneNumber == user.PhoneNumber))
                {
                    var result = Result<UserResDto>.BadRequest(string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Phone number"));
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errors)));
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(user.FullName) && conflicts.Any(u => u.FullName == user.FullName))
                {
                    var result = Result<UserResDto>.BadRequest(string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Full name"));
                    result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errors)));
                    return result;
                }

                var fallback = Result<UserResDto>.BadRequest("A conflicting user already exists in the specified bank.");
                fallback.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errors)));
                return fallback;
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

            try
            {
                // Ensure normalized fields are set so validators and DB comparisons work as expected
                entity.NormalizedUserName = _userManager.NormalizeName(entity.UserName ?? string.Empty);
                entity.NormalizedEmail = _userManager.NormalizeEmail(entity.Email ?? string.Empty);

                var identityResult = await _userManager.CreateAsync(entity, user.Password!);
                if (!identityResult.Succeeded)
                {
                    var errors = identityResult.Errors.Select(e => e.Description);
                    var resultErrors = errors.Select(d => new ResultError(ErrorType.Validation, d));
                    _logger.LogWarning("User creation failed for {Username}: {Errors}", user.Username, string.Join("; ", errors));
                    return Result<UserResDto>.Failure(resultErrors);
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log and return a friendly conflict message. Try to infer field from inner message.
                _logger.LogError(dbEx, "Database update exception while creating user: {Username}", user.Username);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                var msg = "A conflicting user already exists in the specified bank.";

                var innerLower = inner?.ToLowerInvariant() ?? string.Empty;
                if (innerLower.Contains("username") || innerLower.Contains("user_name") || innerLower.Contains("username"))
                    msg = ApiResponseMessages.Validation.UsernameAlreadyExists;
                else if (innerLower.Contains("email"))
                    msg = ApiResponseMessages.Validation.EmailAlreadyExists;
                else if (innerLower.Contains("nationalid"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "National ID");
                else if (innerLower.Contains("phonenumber") || innerLower.Contains("phone_number"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Phone number");
                else if (innerLower.Contains("fullname") || innerLower.Contains("full_name"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Full name");

                var result = Result<UserResDto>.BadRequest(msg);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserCreationFailed, user.Username, string.Join(", ", errors)));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user: {Username}", user.Username);
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Infrastructure.UnexpectedErrorDetailed);
            }

            if (targetRole != null && !string.IsNullOrEmpty(targetRole.Name))
            {
                var roleResult = await _userManager.AddToRoleAsync(entity, targetRole.Name);
                    if (!roleResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(entity);
                        var errors = roleResult.Errors.Select(e => e.Description);
                        return Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                    }
            }

            var createdUser = await _userManager.Users
                .Include(u => u.Bank)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == entity.Id);

            var userDto = _mapper.Map<UserResDto>(createdUser ?? entity);
            return Result<UserResDto>.Success(userDto);
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
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
            }
            // Normalize incoming values (trim) and map into entity
            var normalizedEmail = user.Email?.Trim();
            var normalizedUsername = user.Username?.Trim();
            var normalizedNationalId = string.IsNullOrWhiteSpace(user.NationalId) ? null : user.NationalId.Trim();
            var normalizedPhone = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber.Trim();
            var normalizedFullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim();

            // Map other fields via AutoMapper then override with normalized values
            _mapper.Map(user, existingUser);

            if (normalizedEmail != null) existingUser.Email = normalizedEmail;
            if (normalizedUsername != null) existingUser.UserName = normalizedUsername;
            if (normalizedNationalId != null) existingUser.NationalId = normalizedNationalId;
            if (normalizedPhone != null) existingUser.PhoneNumber = normalizedPhone;
            if (normalizedFullName != null) existingUser.FullName = normalizedFullName;

            try
            {
                var identityResult = await _userManager.UpdateAsync(existingUser);
                if (!identityResult.Succeeded)
                {
                    var errors = identityResult.Errors.Select(e => e.Description);
                    return Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update exception while updating user: {UserId}", userId);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                var innerLower = inner?.ToLowerInvariant() ?? string.Empty;
                var msg = "A conflicting user already exists in the specified bank.";

                if (innerLower.Contains("username") || innerLower.Contains("user_name"))
                    msg = ApiResponseMessages.Validation.UsernameAlreadyExists;
                else if (innerLower.Contains("email"))
                    msg = ApiResponseMessages.Validation.EmailAlreadyExists;
                else if (innerLower.Contains("nationalid"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "National ID");
                else if (innerLower.Contains("phonenumber") || innerLower.Contains("phone_number"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Phone number");
                else if (innerLower.Contains("fullname") || innerLower.Contains("full_name"))
                    msg = string.Format(ApiResponseMessages.Validation.UserConflictExistsFormat, "Full name");

                var result = Result<UserResDto>.BadRequest(msg);
                result.OnFailure(errors => _logger.LogWarning(ApiResponseMessages.Logging.UserUpdateFailed, userId, string.Join(", ", errors)));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user: {UserId}", userId);
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Infrastructure.UnexpectedErrorDetailed);
            }

            return Result<UserResDto>.Success(_mapper.Map<UserResDto>(existingUser));
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
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
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
                return Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
            }

            return Result<UserResDto>.Success(_mapper.Map<UserResDto>(existingUser));
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
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.UserNotFound);
            }

            var userDto = _mapper.Map<UserResDto>(existingUser); // Map before deletion

            var identityResult = await _userManager.DeleteAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<UserResDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
            }

            return Result<UserResDto>.Success(userDto);
        }

        public async Task<Result<bool>> DeleteRangeOfUsersAsync(IEnumerable<string> userIds)
        {
            var userIdsList = userIds.ToList();
            if (!userIdsList.Any())
                return Result<bool>.BadRequest(ApiResponseMessages.Validation.NoUserIdsProvided);

            foreach (var userId in userIdsList)
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                if (existingUser != null)
                {
                    var identityResult = await _userManager.DeleteAsync(existingUser);
                    if (!identityResult.Succeeded)
                        return Result<bool>.Failure(identityResult.Errors.Select(e => new ResultError(ErrorType.Validation, e.Description)));
                }
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result> SetUserActiveStatusAsync(string userId, bool isActive)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateStringInput(userId, "User ID");
            if (validationResult.IsFailure)
                return Result.Failure(validationResult.ErrorItems);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.BadRequest(ApiResponseMessages.Validation.UserNotFound);

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
                return Result.Failure(identityResult.Errors.Select(e => new ResultError(ErrorType.Validation, e.Description)));

            return Result.Success();
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
