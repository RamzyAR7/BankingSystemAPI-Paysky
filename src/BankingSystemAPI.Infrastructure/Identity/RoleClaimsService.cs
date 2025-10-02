using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleClaimsService : IRoleClaimsService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleClaimsService(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<Result<RoleClaimsUpdateResultDto>> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto)
        {
            // Input validation
            if (dto == null || string.IsNullOrWhiteSpace(dto.RoleName))
            {
                return Result<RoleClaimsUpdateResultDto>.Failure(new[] { "Role name cannot be null or empty." });
            }

            if (dto.Claims == null)
            {
                return Result<RoleClaimsUpdateResultDto>.Failure(new[] { "Claims list cannot be null." });
            }

            var role = await _roleManager.FindByNameAsync(dto.RoleName);
            if (role == null)
            {
                return Result<RoleClaimsUpdateResultDto>.Failure(new[] { "Role not found." });
            }

            // REMOVE existing claims
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            if (existingClaims.Any())
            {
                foreach (var claim in existingClaims)
                {
                    var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                    if (!removeResult.Succeeded)
                    {
                        var errors = removeResult.Errors.Select(e => e.Description);
                        return Result<RoleClaimsUpdateResultDto>.Failure(errors);
                    }
                }
            }

            // ADD new claims
            var distinctClaims = dto.Claims.Distinct().ToList();
            foreach (var claim in distinctClaims)
            {
                if (string.IsNullOrWhiteSpace(claim)) continue; // Skip empty claims
                
                var addResult = await _roleManager.AddClaimAsync(role, new Claim("Permission", claim));
                if (!addResult.Succeeded)
                {
                    var errors = addResult.Errors.Select(e => e.Description);
                    return Result<RoleClaimsUpdateResultDto>.Failure(errors);
                }
            }

            var result = new RoleClaimsUpdateResultDto
            {
                RoleClaims = new RoleClaimsResDto
                {
                    Name = role.Name,
                    Claims = distinctClaims
                },
                Succeeded = true,
                Errors = new List<IdentityError>()
            };

            return Result<RoleClaimsUpdateResultDto>.Success(result);
        }

        public async Task<Result<ICollection<RoleClaimsResDto>>> GetAllClaimsByGroup()
        {
            try
            {
                var result = new List<RoleClaimsResDto>();

                var controllers = Enum.GetValues(typeof(ControllerType)).Cast<ControllerType>();
                foreach (var controller in controllers)
                {
                    IEnumerable<string> permissions = controller switch
                    {
                        ControllerType.User => new[]
                        {
                            Permission.User.Create,
                            Permission.User.Update,
                            Permission.User.Delete,
                            Permission.User.ReadAll,
                            Permission.User.ReadById,
                            Permission.User.ReadByUsername,
                            Permission.User.ChangePassword,
                            Permission.User.DeleteRange,
                            Permission.User.ReadSelf,
                            Permission.User.UpdateActiveStatus
                        },
                        ControllerType.UserRoles => new[]
                        {
                            Permission.UserRoles.Assign
                        },
                        ControllerType.Role => new[]
                        {
                            Permission.Role.Create,
                            Permission.Role.Delete,
                            Permission.Role.ReadAll
                        },
                        ControllerType.RoleClaims => new[]
                        {
                            Permission.RoleClaims.Assign,
                            Permission.RoleClaims.ReadAll
                        },
                        ControllerType.Auth => new[]
                        {
                            Permission.Auth.RevokeToken
                        },
                        ControllerType.Account => new[]
                        {
                            Permission.Account.ReadById,
                            Permission.Account.ReadByNationalId,
                            Permission.Account.ReadByAccountNumber,
                            Permission.Account.ReadByUserId,
                            Permission.Account.Delete,
                            Permission.Account.DeleteMany,
                            Permission.Account.UpdateActiveStatus
                        },
                        ControllerType.CheckingAccount => new[]
                        {
                            Permission.CheckingAccount.Create,
                            Permission.CheckingAccount.Update,
                            Permission.CheckingAccount.ReadAll,
                            Permission.CheckingAccount.UpdateActiveStatus
                        },
                        ControllerType.SavingsAccount => new[]
                        {
                            Permission.SavingsAccount.Create,
                            Permission.SavingsAccount.Update,
                            Permission.SavingsAccount.ReadAll,
                            Permission.SavingsAccount.UpdateActiveStatus,
                            Permission.SavingsAccount.ReadAllInterestRate,
                            Permission.SavingsAccount.ReadInterestRateById
                        },
                        ControllerType.Currency => new[]
                        {
                            Permission.Currency.Create,
                            Permission.Currency.Update,
                            Permission.Currency.Delete,
                            Permission.Currency.ReadAll,
                            Permission.Currency.ReadById
                        },
                        ControllerType.Transaction => new[]
                        {
                            Permission.Transaction.ReadBalance,
                            Permission.Transaction.Deposit,
                            Permission.Transaction.Withdraw,
                            Permission.Transaction.Transfer,
                            Permission.Transaction.ReadAllHistory,
                            Permission.Transaction.ReadById
                        },
                        ControllerType.Bank => new[]
                        {
                            Permission.Bank.Create,
                            Permission.Bank.Update,
                            Permission.Bank.Delete,
                            Permission.Bank.ReadAll,
                            Permission.Bank.ReadById,
                            Permission.Bank.ReadByName,
                            Permission.Bank.UpdateActiveStatus
                        },
                        _ => Array.Empty<string>()
                    };

                    result.Add(new RoleClaimsResDto
                    {
                        Name = controller.ToString(),
                        Claims = permissions.Distinct().ToList()
                    });
                }

                return Result<ICollection<RoleClaimsResDto>>.Success(result);
            }
            catch (System.Exception ex)
            {
                return Result<ICollection<RoleClaimsResDto>>.Failure(new[] { $"Failed to retrieve claims by group: {ex.Message}" });
            }
        }
    }
}
