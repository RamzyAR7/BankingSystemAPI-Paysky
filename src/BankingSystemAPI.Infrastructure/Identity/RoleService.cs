#region Usings
using AutoMapper;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<RoleResDto>>> GetAllRolesAsync()
        {
            try
            {
                var rolesResult = await LoadAllRolesAsync();
                if (rolesResult.IsFailure)
                    return Result<List<RoleResDto>>.Failure(rolesResult.ErrorItems);

                var enrichedRolesResult = await EnrichRolesWithClaimsAsync(rolesResult.Value!);

                // Add side effects using ResultExtensions
                enrichedRolesResult.OnSuccess(() =>
                    {
                        _logger.LogDebug(ApiResponseMessages.Logging.RoleRetrieved, enrichedRolesResult.Value!.Count);
                    })
                    .OnFailure(errors =>
                    {
                        _logger.LogWarning(ApiResponseMessages.Logging.RoleRetrieveFailed,
                            string.Join(", ", errors));
                    });

                return enrichedRolesResult;
            }
            catch (System.Exception ex)
            {
                var errorMessage = string.Format(ApiResponseMessages.Logging.RoleRetrieveError, ex.Message);
                _logger.LogError(ex, errorMessage);
                return Result<List<RoleResDto>>.BadRequest(errorMessage);
            }
        }

        public async Task<Result<RoleUpdateResultDto>> CreateRoleAsync(RoleReqDto dto)
        {
            // Chain validation and creation using ResultExtensions
            var validationResult = ValidateCreateRoleInput(dto);
            if (validationResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(validationResult.ErrorItems);

            var uniquenessResult = await ValidateRoleUniquenessAsync(dto.Name);
            if (uniquenessResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(uniquenessResult.ErrorItems);

            var createResult = await ExecuteRoleCreationAsync(dto);

            // Add side effects using ResultExtensions
            createResult.OnSuccess(() =>
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.RoleCreated, dto.Name);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning(ApiResponseMessages.Logging.RoleCreateFailed,
                        dto.Name, string.Join(", ", errors));
                });

            return createResult;
        }

        public async Task<Result<RoleUpdateResultDto>> DeleteRoleAsync(string roleId)
        {
            // Chain validation and deletion using ResultExtensions
            var validationResult = ValidateDeleteRoleInput(roleId);
            if (validationResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(validationResult.ErrorItems);

            var roleResult = await FindRoleForDeletionAsync(roleId);
            if (roleResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(roleResult.ErrorItems);

            var deleteResult = await ExecuteRoleDeletionAsync(roleResult.Value!);

            // Add side effects using ResultExtensions
            deleteResult.OnSuccess(() =>
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.RoleDeleted,
                        roleId, roleResult.Value!.Name);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning(ApiResponseMessages.Logging.RoleDeleteFailed,
                        roleId, string.Join(", ", errors));
                });

            return deleteResult;
        }

        public async Task<Result<bool>> IsRoleInUseAsync(string roleId)
        {
            // Validate input using ResultExtensions
            var validationResult = ValidateRoleIdInput(roleId);
            if (validationResult.IsFailure)
                return Result<bool>.Failure(validationResult.ErrorItems);

            try
            {
                // Check both FK relationship and UserRoles table using functional patterns
                var fkUsageResult = await CheckForeignKeyUsageAsync(roleId);
                var roleTableUsageResult = await CheckRoleTableUsageAsync(roleId);

                // Combine results using ResultExtensions
                var combinedUsage = fkUsageResult.Value || roleTableUsageResult.Value;
                var result = Result<bool>.Success(combinedUsage);

                // Add side effects using ResultExtensions
                result.OnSuccess(() =>
                    _logger.LogDebug(ApiResponseMessages.Logging.RoleUsageCheckCompleted,
                        roleId, combinedUsage, fkUsageResult.Value, roleTableUsageResult.Value));

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = Result<bool>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                errorResult.OnFailure(errors =>
                    _logger.LogError(ex, ApiResponseMessages.Logging.RoleUsageCheckFailed, roleId));
                return errorResult;
            }
        }

        #region Private Helper Methods

        private async Task<Result<List<ApplicationRole>>> LoadAllRolesAsync()
        {
            try
            {
                var roles = await Task.FromResult(_roleManager.Roles.ToList());
                return Result<List<ApplicationRole>>.Success(roles);
            }
            catch (Exception ex)
            {
                return Result<List<ApplicationRole>>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Result<List<RoleResDto>>> EnrichRolesWithClaimsAsync(List<ApplicationRole> roles)
        {
            try
            {
                var roleDtos = _mapper.Map<List<RoleResDto>>(roles);

                // Populate claims for each role
                for (int i = 0; i < roleDtos.Count; i++)
                {
                    var dto = roleDtos[i];
                    var role = await _roleManager.FindByNameAsync(dto.Name);
                    if (role == null) continue;

                    var claims = await _roleManager.GetClaimsAsync(role);
                    dto.Claims = claims.Where(c => c.Type == "Permission").Select(c => c.Value).Distinct().ToList();
                }

                return Result<List<RoleResDto>>.Success(roleDtos);
            }
            catch (Exception ex)
            {
                return Result<List<RoleResDto>>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private Result ValidateCreateRoleInput(RoleReqDto dto)
        {
            return dto.ToResult(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Role"))
                .Bind(d => string.IsNullOrWhiteSpace(d.Name)
                    ? Result.BadRequest(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role name"))
                    : Result.Success());
        }

        private async Task<Result> ValidateRoleUniquenessAsync(string roleName)
        {
            var existingRole = await _roleManager.FindByNameAsync(roleName);
            return existingRole == null
                ? Result.Success()
                : Result.BadRequest(string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, "Role", roleName));
        }

        private async Task<Result<RoleUpdateResultDto>> ExecuteRoleCreationAsync(RoleReqDto dto)
        {
            try
            {
                var role = new ApplicationRole { Name = dto.Name };
                var identityResult = await _roleManager.CreateAsync(role);

                if (!identityResult.Succeeded)
                {
                    var errors = identityResult.Errors.Select(e => e.Description);
                    return Result<RoleUpdateResultDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                }

                var result = new RoleUpdateResultDto
                {
                    Operation = "Create",
                    Role = _mapper.Map<RoleResDto>(role)
                };

                return Result<RoleUpdateResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<RoleUpdateResultDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private Result ValidateDeleteRoleInput(string roleId)
        {
            return string.IsNullOrWhiteSpace(roleId)
                ? Result.BadRequest(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role ID"))
                : Result.Success();
        }

        private Result ValidateRoleIdInput(string roleId)
        {
            return string.IsNullOrWhiteSpace(roleId)
                ? Result.BadRequest(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role ID"))
                : Result.Success();
        }

        private async Task<Result<ApplicationRole>> FindRoleForDeletionAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            return role.ToResult(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, "Role", roleId));
        }

        private async Task<Result<RoleUpdateResultDto>> ExecuteRoleDeletionAsync(ApplicationRole role)
        {
            try
            {
                var identityResult = await _roleManager.DeleteAsync(role);

                if (!identityResult.Succeeded)
                {
                    var errors = identityResult.Errors.Select(e => e.Description);
                    return Result<RoleUpdateResultDto>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                }

                var result = new RoleUpdateResultDto
                {
                    Operation = "Delete",
                    Role = _mapper.Map<RoleResDto>(role)
                };

                return Result<RoleUpdateResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<RoleUpdateResultDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private async Task<Result<bool>> CheckForeignKeyUsageAsync(string roleId)
        {
            try
            {
                var hasUsersViaFk = await _userManager.Users.AnyAsync(u => u.RoleId == roleId);
                return Result<bool>.Success(hasUsersViaFk);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ApiResponseMessages.Logging.RoleFkCheckFailed, roleId);
                return Result<bool>.Success(false); // Fail-safe: assume not in use
            }
        }

        private async Task<Result<bool>> CheckRoleTableUsageAsync(string roleId)
        {
            try
            {
                // Get role name to check UserRoles table
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                    return Result<bool>.Success(false);

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                return Result<bool>.Success(usersInRole.Any());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ApiResponseMessages.Logging.RoleTableCheckFailed, roleId);
                return Result<bool>.Success(false); // Fail-safe: assume not in use
            }
        }

        #endregion
    }
}

