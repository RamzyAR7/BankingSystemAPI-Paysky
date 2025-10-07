#region Usings
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
#endregion


namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleClaimsService : IRoleClaimsService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<RoleClaimsService> _logger;

        public RoleClaimsService(RoleManager<ApplicationRole> roleManager, ILogger<RoleClaimsService> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<Result<RoleClaimsUpdateResultDto>> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto)
        {
            // Chain validation and update operations using ResultExtensions
            return await ValidateInputAsync(dto)
                .BindAsync(async validDto => await FindRoleAsync(validDto.RoleName))
                .BindAsync(async role => await RemoveExistingClaimsAsync(role))
                .BindAsync(async role => await AddNewClaimsAsync(role, dto.Claims))
                .MapAsync(role => Task.FromResult(CreateSuccessResultAsync(role, dto.Claims)))
                .OnSuccess(() =>
                {
                    _logger.LogInformation("Role claims updated successfully for role: {RoleName}", dto.RoleName);
                })
                .OnFailure(errors =>
                {
                    _logger.LogWarning("Role claims update failed for role: {RoleName}. Errors: {Errors}",
                        dto?.RoleName, string.Join(", ", errors));
                });
        }

        public Task<Result<ICollection<RoleClaimsResDto>>> GetAllClaimsByGroup()
        {
            try
            {
                var result = BuildClaimsByGroup();

                // Add side effects without changing return type
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Successfully retrieved all claims by group");
                }
                else
                {
                    _logger.LogError("Failed to retrieve claims by group. Errors: {Errors}",
                        string.Join(", ", result.Errors));
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message);
                _logger.LogError(ex, errorMessage);
                return Task.FromResult(Result<ICollection<RoleClaimsResDto>>.BadRequest(errorMessage));
            }
        }

        private Task<Result<UpdateRoleClaimsDto>> ValidateInputAsync(UpdateRoleClaimsDto dto)
        {
            var res = dto.ToResult(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Role claims"))
                .Bind(d => string.IsNullOrWhiteSpace(d.RoleName)
                    ? Result<UpdateRoleClaimsDto>.BadRequest(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role name"))
                    : Result<UpdateRoleClaimsDto>.Success(d))
                .Bind(d => d.Claims == null
                    ? Result<UpdateRoleClaimsDto>.BadRequest(ApiResponseMessages.Validation.ClaimsListRequired)
                    : Result<UpdateRoleClaimsDto>.Success(d));

            return Task.FromResult(res);
        }

        private async Task<Result<ApplicationRole>> FindRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            return role.ToResult(string.Format(ApiResponseMessages.BankingErrors.NotFoundFormat, "Role", roleName));
        }

        private async Task<Result<ApplicationRole>> RemoveExistingClaimsAsync(ApplicationRole role)
        {
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            if (!existingClaims.Any())
                return Result<ApplicationRole>.Success(role);

            // Remove all existing claims
            foreach (var claim in existingClaims)
            {
                var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => e.Description);
                    return Result<ApplicationRole>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                }
            }

            return Result<ApplicationRole>.Success(role);
        }

        private async Task<Result<ApplicationRole>> AddNewClaimsAsync(ApplicationRole role, ICollection<string> claims)
        {
            var distinctClaims = claims.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

            foreach (var claim in distinctClaims)
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim("Permission", claim));
                if (!addResult.Succeeded)
                {
                    var errors = addResult.Errors.Select(e => e.Description);
                    return Result<ApplicationRole>.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)));
                }
            }

            return Result<ApplicationRole>.Success(role);
        }

        private RoleClaimsUpdateResultDto CreateSuccessResultAsync(ApplicationRole role, ICollection<string> claims)
        {
            var distinctClaims = claims.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

            return new RoleClaimsUpdateResultDto
            {
                RoleName = role.Name ?? string.Empty,
                UpdatedClaims = distinctClaims
            };
        }

        private Result<ICollection<RoleClaimsResDto>> BuildClaimsByGroup()
        {
            try
            {
                var result = new List<RoleClaimsResDto>();

                var controllers = Enum.GetValues(typeof(ControllerType)).Cast<ControllerType>();
                foreach (var controller in controllers)
                {
                    var permissions = GetPermissionsForController(controller);


                    result.Add(new RoleClaimsResDto
                    {
                        Name = controller.ToString(),
                        Claims = permissions.Distinct().ToList()
                    });
                }

                return Result<ICollection<RoleClaimsResDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<ICollection<RoleClaimsResDto>>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }

        private IEnumerable<string> GetPermissionsForController(ControllerType controller)
        {
            return controller switch
            {
                ControllerType.User => new[]
                {
                    Permission.User.Create, Permission.User.Update, Permission.User.Delete,
                    Permission.User.ReadAll, Permission.User.ReadById, Permission.User.ReadByUsername,
                    Permission.User.ChangePassword, Permission.User.DeleteRange, Permission.User.ReadSelf,
                    Permission.User.UpdateActiveStatus
                },
                ControllerType.UserRoles => new[] { Permission.UserRoles.Assign },
                ControllerType.Role => new[]
                {
                    Permission.Role.Create, Permission.Role.Delete, Permission.Role.ReadAll
                },
                ControllerType.RoleClaims => new[]
                {
                    Permission.RoleClaims.Assign, Permission.RoleClaims.ReadAll
                },
                ControllerType.Auth => new[] { Permission.Auth.RevokeToken },
                ControllerType.Account => new[]
                {
                    Permission.Account.ReadById, Permission.Account.ReadByNationalId,
                    Permission.Account.ReadByAccountNumber, Permission.Account.ReadByUserId,
                    Permission.Account.Delete, Permission.Account.DeleteMany, Permission.Account.UpdateActiveStatus
                },
                ControllerType.CheckingAccount => new[]
                {
                    Permission.CheckingAccount.Create, Permission.CheckingAccount.Update,
                    Permission.CheckingAccount.ReadAll, Permission.CheckingAccount.UpdateActiveStatus
                },
                ControllerType.SavingsAccount => new[]
                {
                    Permission.SavingsAccount.Create, Permission.SavingsAccount.Update,
                    Permission.SavingsAccount.ReadAll, Permission.SavingsAccount.UpdateActiveStatus,
                    Permission.SavingsAccount.ReadAllInterestRate, Permission.SavingsAccount.ReadInterestRateById
                },
                ControllerType.Currency => new[]
                {
                    Permission.Currency.Create, Permission.Currency.Update, Permission.Currency.Delete,
                    Permission.Currency.ReadAll, Permission.Currency.ReadById
                },
                ControllerType.Transaction => new[]
                {
                    Permission.Transaction.ReadBalance, Permission.Transaction.Deposit,
                    Permission.Transaction.Withdraw, Permission.Transaction.Transfer,
                    Permission.Transaction.ReadAllHistory, Permission.Transaction.ReadById
                },
                ControllerType.Bank => new[]
                {
                    Permission.Bank.Create, Permission.Bank.Update, Permission.Bank.Delete,
                    Permission.Bank.ReadAll, Permission.Bank.ReadById, Permission.Bank.ReadByName,
                    Permission.Bank.UpdateActiveStatus
                },
                _ => Array.Empty<string>()
            };
        }
    }
}

