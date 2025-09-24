using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using BankingSystemAPI.Domain.Constant;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;


namespace BankingSystemAPI.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IBankAuthorizationHelper? _bankAuth;
        private readonly IUnitOfWork? _uow;
        public UserService(UserManager<ApplicationUser> userManager, IMapper mapper, ICurrentUserService currentUserService, RoleManager<ApplicationRole> roleManager, IUnitOfWork? uow = null, IBankAuthorizationHelper? bankAuth = null)
        {
            _userManager = userManager;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _roleManager = roleManager;
            _bankAuth = bankAuth;
            _uow = uow;
        }

        private async Task<UserResDto> MapUserDtoAsync(ApplicationUser user)
        {
            var dto = _mapper.Map<UserResDto>(user);
            var roles = await _userManager.GetRolesAsync(user);
            dto.Role = roles.FirstOrDefault();
            // populate BankName if not present from navigation
            if (_uow != null && string.IsNullOrEmpty(dto.BankName) && user.BankId.HasValue)
            {
                var bank = await _uow.BankRepository.GetByIdAsync(user.BankId.Value);
                dto.BankName = bank?.Name;
            }
            return dto;
        }

        public async Task<IList<UserResDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            var allUsers = await _userManager.Users.Include(u => u.Accounts).Include(u => u.Bank).ToListAsync();

            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterUsersAsync(allUsers);
                allUsers = filtered.ToList();
            }
            
            var paged = allUsers.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var userDtos = new List<UserResDto>();
            foreach (var user in paged)
            {
                var dto = await MapUserDtoAsync(user);
                userDtos.Add(dto);
            }
            return userDtos;
        }
        public async Task<UserResDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return null;

            if (_bankAuth != null)
                await _bankAuth.EnsureCanAccessUserAsync(user.Id);

            var dto = await MapUserDtoAsync(user);
            return dto;
        }
        public async Task<UserResDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            if (_bankAuth != null)
                await _bankAuth.EnsureCanAccessUserAsync(userId);

            var dto = await MapUserDtoAsync(user);
            return dto;
        }
        public async Task<UserUpdateResultDto> CreateUserAsync(UserReqDto user)
        {
            var result = new UserUpdateResultDto();
            // Acting user info
            var actingUserId = _currentUserService.UserId;
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(actingRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isClient = string.Equals(actingRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);

            int? targetBankId = null;
            string targetRole = UserRole.Client.ToString();

            if (isSuperAdmin)
            {
                // SuperAdmin-created users must specify a bank and a role (cannot create another SuperAdmin)
                if (user.BankId == 0 || user.BankId == null)
                {
                    result.Errors.Add(new IdentityError { Description = "BankId is required for Super Admin created users." });
                    result.Succeeded = false;
                    return result;
                }
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    result.Errors.Add(new IdentityError { Description = "Role is required for Super Admin created users." });
                    result.Succeeded = false;
                    return result;
                }
                var validRole = await _roleManager.RoleExistsAsync(user.Role);
                if (!validRole || user.Role.Equals(UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new IdentityError { Description = "Invalid or forbidden role specified." });
                    result.Succeeded = false;
                    return result;
                }
                targetBankId = user.BankId;
                targetRole = user.Role;
            }
            else if (!isClient)
            {
                var actingUser = await _userManager.FindByIdAsync(actingUserId);
                if (actingUser == null)
                {
                    result.Errors.Add(new IdentityError { Description = "Acting user not found." });
                    result.Succeeded = false;
                    return result;
                }

                // If acting user has no bank, perform a global duplicate check and return duplicate error if found
                if (!actingUser.BankId.HasValue || actingUser.BankId.Value == 0)
                {
                    var existsGlobal = await _userManager.Users.AnyAsync(u =>
                        u.UserName == user.Username ||
                        u.Email == user.Email ||
                        u.NationalId == user.NationalId ||
                        u.PhoneNumber == user.PhoneNumber);

                    if (existsGlobal)
                    {
                        result.Errors.Add(new IdentityError { Description = "User with same details already exists." });
                        result.Succeeded = false;
                        return result;
                    }

                    // Acting user isn't associated with a bank, cannot determine target bank for creation
                    result.Errors.Add(new IdentityError { Description = "Acting user is not associated with a bank." });
                    result.Succeeded = false;
                    return result;
                }

                targetBankId = actingUser.BankId;
                targetRole = UserRole.Client.ToString();
            }
            else
            {
                result.Errors.Add(new IdentityError { Description = "Clients are not allowed to create users." });
                result.Succeeded = false;
                return result;
            }

            // At this point we must have a valid bank
            if (!targetBankId.HasValue || targetBankId.Value == 0)
            {
                result.Errors.Add(new IdentityError { Description = "A valid BankId is required." });
                result.Succeeded = false;
                return result;
            }

            // Ensure bank exists if UnitOfWork available
            if (_uow != null)
            {
                var bank = await _uow.BankRepository.GetByIdAsync(targetBankId.Value);
                if (bank == null)
                {
                    result.Errors.Add(new IdentityError { Description = "Specified bank does not exist." });
                    result.Succeeded = false;
                    return result;
                }
            }

            // Scoped duplicate checks within the target bank
            var exists = await _userManager.Users.AnyAsync(u =>
                u.BankId.HasValue && u.BankId.Value == targetBankId.Value &&
                (u.UserName == user.Username ||
                 u.Email == user.Email ||
                 u.NationalId == user.NationalId ||
                 u.PhoneNumber == user.PhoneNumber));

            if (exists)
            {
                result.Errors.Add(new IdentityError { Description = "User with same details already exists in this bank." });
                result.Succeeded = false;
                return result;
            }

            // Map entity
            var entity = _mapper.Map<ApplicationUser>(user);
            entity.BankId = targetBankId.Value;

            // Create user
            var identityResult = await _userManager.CreateAsync(entity, user.Password);
            if (!identityResult.Succeeded)
            {
                result.Errors.AddRange(identityResult.Errors);
                result.Succeeded = false;
                return result;
            }

            // Assign role
            var roleResult = await _userManager.AddToRoleAsync(entity, targetRole);
            if (!roleResult.Succeeded)
            {
                result.Errors.AddRange(roleResult.Errors);
                result.Succeeded = false;
                return result;
            }

            // Prepare result
            // Map to DTO and populate role/bank consistently
            result.User = await MapUserDtoAsync(entity);
            result.User.Role = targetRole;
            result.Succeeded = true;
            return result;
        }

        public async Task<UserUpdateResultDto> UpdateUserAsync(string userId, UserEditDto user)
        {
            var result = new UserUpdateResultDto();
            if (string.IsNullOrWhiteSpace(userId))
            {
                result.Errors.Add(new IdentityError { Description = "User ID cannot be null or empty." });
                result.Succeeded = false;
                return result;
            }
            var existingUser = await _userManager.Users.Include(u => u.Accounts).Include(u => u.Bank).FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            if (_bankAuth != null)
                // ensure caller is allowed to edit this user
                await _bankAuth.EnsureCanModifyUserAsync(userId, UserModificationOperation.Edit);

            var duplicate = await _userManager.Users.AnyAsync(u =>
                u.Id != userId &&
                u.BankId == existingUser.BankId &&
                (u.UserName == user.Username ||
                 u.Email == user.Email ||
                 u.NationalId == user.NationalId ||
                 u.PhoneNumber == user.PhoneNumber));
            if (duplicate)
            {
                result.Errors.Add(new IdentityError { Description = "Another user with the same details already exists in this bank." });
                result.Succeeded = false;
                return result;
            }

            // Map changes
            _mapper.Map(user, existingUser);

            var identityResult = await _userManager.UpdateAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                result.Errors.AddRange(identityResult.Errors);
                result.Succeeded = false;
                return result;
            }
            result.User = await MapUserDtoAsync(existingUser);
             result.Succeeded = true;
             return result;
         }
         public async Task<UserUpdateResultDto> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto)
        {
            var result = new UserUpdateResultDto();
            var existingUser = await _userManager.Users.Include(u => u.Bank).FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            if (_bankAuth != null)
                await _bankAuth.EnsureCanModifyUserAsync(userId, UserModificationOperation.ChangePassword);

            // Acting user info
            var actingUserId = _currentUserService.UserId;
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(actingRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isClient = string.Equals(actingRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);
            var isSelf = !string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, userId, StringComparison.OrdinalIgnoreCase);

            // Get target user's roles
            var targetRoles = await _userManager.GetRolesAsync(existingUser);
            var isTargetClient = targetRoles.Contains(UserRole.Client.ToString());

            if (isSuperAdmin)
            {
                // SuperAdmin can reset any password
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetRes = await _userManager.ResetPasswordAsync(existingUser, token, dto.NewPassword);
                if (!resetRes.Succeeded)
                {
                    result.Errors.AddRange(resetRes.Errors);
                    result.Succeeded = false;
                    return result;
                }
                result.User = await MapUserDtoAsync(existingUser);
                result.Succeeded = true;
                return result;
            }

            if (!isClient && !isSuperAdmin && isTargetClient && !isSelf)
            {
                // Admin or any non-client/non-superadmin role can reset password of a client without current password
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetRes = await _userManager.ResetPasswordAsync(existingUser, token, dto.NewPassword);
                if (!resetRes.Succeeded)
                {
                    result.Errors.AddRange(resetRes.Errors);
                    result.Succeeded = false;
                    return result;
                }
                result.User = await MapUserDtoAsync(existingUser);
                result.Succeeded = true;
                return result;
            }

            if (isSelf)
            {
                // Self-change requires current password
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                {
                    result.Errors.Add(new IdentityError { Description = "Current password is required." });
                    result.Succeeded = false;
                    return result;
                }
                var changeResult = await _userManager.ChangePasswordAsync(existingUser, dto.CurrentPassword, dto.NewPassword);
                if (!changeResult.Succeeded)
                {
                    result.Errors.AddRange(changeResult.Errors);
                    result.Succeeded = false;
                    return result;
                }
                result.User = await MapUserDtoAsync(existingUser);
                result.Succeeded = true;
                return result;
            }

            // Only SuperAdmin, self, or admin/non-client for client can change password
            result.Errors.Add(new IdentityError { Description = "You are not authorized to change this user's password." });
            result.Succeeded = false;
            return result;
        }
        public async Task<UserUpdateResultDto> DeleteUserAsync(string userId)
        {
            var result = new UserUpdateResultDto();
            var existingUser = await _userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                result.Errors.Add(new IdentityError { Description = "User not found." });
                result.Succeeded = false;
                return result;
            }

            if (_bankAuth != null)
                await _bankAuth.EnsureCanModifyUserAsync(userId, UserModificationOperation.Delete);

            var actingUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new IdentityError { Description = "Cannot delete yourself." });
                result.Succeeded = false;
                return result;
            }

            // Prevent deleting user with accounts
            if (existingUser.Accounts != null && existingUser.Accounts.Any())
            {
                result.Errors.Add(new IdentityError { Description = "Cannot delete user with existing accounts." });
                result.Succeeded = false;
                return result;
            }

            var identityResult = await _userManager.DeleteAsync(existingUser);
            if (!identityResult.Succeeded)
            {
                result.Errors.AddRange(identityResult.Errors);
                result.Succeeded = false;
                return result;
            }
            result.User = await MapUserDtoAsync(existingUser);
             result.Succeeded = true;
             return result;
        }
        public async Task<UserUpdateResultDto> DeleteRangeOfUsersAsync(IEnumerable<string> userIds)
        {
            var result = new UserUpdateResultDto();
            foreach (var userId in userIds)
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                if (existingUser != null)
                {
                    if (_bankAuth != null)
                        await _bankAuth.EnsureCanModifyUserAsync(userId, UserModificationOperation.Delete);

                    var identityResult = await _userManager.DeleteAsync(existingUser);
                    if (!identityResult.Succeeded)
                    {
                        result.Errors.AddRange(identityResult.Errors);
                        result.Succeeded = false;
                        return result;
                    }
                }
            }
            result.Succeeded = true;
            return result;
        }
        public async Task<UserResDto?> GetCurrentUserInfoAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Accounts)
                .Include(u => u.Bank)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            if (_bankAuth != null)
                await _bankAuth.EnsureCanAccessUserAsync(userId);

            var dto = await MapUserDtoAsync(user);
            return dto;
        }

        public async Task SetUserActiveStatusAsync(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException($"User with ID '{userId}' not found.");

            if (_bankAuth != null)
                await _bankAuth.EnsureCanModifyUserAsync(userId, UserModificationOperation.Edit);

            user.IsActive = isActive;
            await _userManager.UpdateAsync(user);
        }

        public async Task<IList<UserResDto>> GetUsersByBankIdAsync(int bankId)
        {
            if (_bankAuth != null)
            {
                var isClient = await _bankAuth.IsClientAsync();
                if (isClient)
                    return new List<UserResDto>();
            }

            var isSuper = _bankAuth == null ? false : await _bankAuth.IsSuperAdminAsync();

            List<ApplicationUser> users;
            if (isSuper)
            {
                // SuperAdmin should see all users regardless of bankId
                users = await _userManager.Users
                    .Include(u => u.Accounts)
                    .Include(u => u.Bank)
                    .ToListAsync();
            }
            else
            {
                // Only allow searching by the acting user's own bank
                var actingUser =  await _bankAuth.GetActingUserAsync();
                if (actingUser == null)
                    return new List<UserResDto>();
                bankId = actingUser.BankId ?? 0;

                users = await _userManager.Users
                    .Include(u => u.Accounts)
                    .Include(u => u.Bank)
                    .Where(u => u.BankId == bankId)
                    .ToListAsync();
            }

            // Apply filtering to the collection to ensure consistent rules
            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterUsersAsync(users);
                users = filtered.ToList();
            }

            var userDtos = new List<UserResDto>();
            foreach (var user in users)
            {
                var dto = await MapUserDtoAsync(user);
                userDtos.Add(dto);
            }
            return userDtos;
        }

         public async Task<IList<UserResDto>> GetUsersByBankNameAsync(string bankName)
        {
            if (_bankAuth != null)
            {
                var isClient = await _bankAuth.IsClientAsync();
                if (isClient)
                    return new List<UserResDto>();
            }

            var isSuper = _bankAuth == null ? false : await _bankAuth.IsSuperAdminAsync();

            List<ApplicationUser> users;
            if (isSuper)
            {
                if (string.IsNullOrWhiteSpace(bankName))
                {
                    users = await _userManager.Users
                        .Include(u => u.Accounts)
                        .Include(u => u.Bank)
                        .ToListAsync();
                }
                else
                {
                    users = await _userManager.Users
                        .Include(u => u.Accounts)
                        .Include(u => u.Bank)
                        .Where(u => u.Bank != null && u.Bank.Name == bankName)
                        .ToListAsync();
                }
            }
            else
            {
                var actingUser = _bankAuth == null ? null : await _bankAuth.GetActingUserAsync();
                if (actingUser == null || actingUser.Bank == null)
                    return new List<UserResDto>();
                bankName = actingUser.Bank.Name;

                users = await _userManager.Users
                    .Include(u => u.Accounts)
                    .Include(u => u.Bank)
                    .Where(u => u.Bank != null && u.Bank.Name == bankName)
                    .ToListAsync();
            }

            // Apply filtering to the collection
            if (_bankAuth != null)
            {
                var filtered = await _bankAuth.FilterUsersAsync(users);
                users = filtered.ToList();
            }

            var userDtos = new List<UserResDto>();
            foreach (var user in users)
            {
                var dto = await MapUserDtoAsync(user);
                userDtos.Add(dto);
            }
            return userDtos;
         }
     }
 }
