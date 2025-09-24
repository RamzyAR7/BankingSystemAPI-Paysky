using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleClaimsService : IRoleClaimsService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public RoleClaimsService(RoleManager<ApplicationRole> roleManager, IHttpContextAccessor httpContextAccessor)
        {
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<RoleClaimsUpdateResultDto> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto)
        {
            var result = new RoleClaimsUpdateResultDto();
            var role = await _roleManager.FindByNameAsync(dto.RoleName);
            if (role == null)
            {
                result.Errors.Add(new IdentityError { Description = "Role not found" });
                result.Succeeded = false;
                return result;
            }

            // Prevent modifying SuperAdmin role claims
            if (role.Name == UserRole.SuperAdmin.ToString())
            {
                result.Errors.Add(new IdentityError { Description = "Cannot modify claims for SuperAdmin role" });
                result.Succeeded = false;
                return result;
            }

            // For Client role, only allow SuperAdmin to modify its claims
            if (role.Name == UserRole.Client.ToString())
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null || !user.IsInRole(UserRole.SuperAdmin.ToString()))
                {
                    result.Errors.Add(new IdentityError { Description = "Only SuperAdmin can modify claims for Client role" });
                    result.Succeeded = false;
                    return result;
                }
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
                        result.Errors.AddRange(removeResult.Errors);
                        result.Succeeded = false;
                        return result;
                    }
                }
            }

            // ADD new claims
            foreach (var claim in dto.Claims.Distinct())
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim("Permission", claim));
                if (!addResult.Succeeded)
                {
                    result.Errors.AddRange(addResult.Errors);
                    result.Succeeded = false;
                    return result;
                }
            }

            result.RoleClaims = new RoleClaimsResDto
            {
                Name = role.Name,
                Claims = dto.Claims.Distinct().ToList()
            };
            result.Succeeded = true;
            return result;
        }

        public async Task<ICollection<RoleClaimsResDto>> GetAllClaimsByGroup()
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
                        Permission.User.ReadSelf
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
                        Permission.Account.DeleteMany
                    },
                    ControllerType.CheckingAccount => new[]
                    {
                        Permission.CheckingAccount.Create,
                        Permission.CheckingAccount.Update,
                        Permission.CheckingAccount.ReadAll
                    },
                    ControllerType.SavingsAccount => new[]
                    {
                        Permission.SavingsAccount.Create,
                        Permission.SavingsAccount.Update,
                        Permission.SavingsAccount.ReadAll,
                        Permission.SavingsAccount.ReadAllInterestRate,
                        Permission.SavingsAccount.ReadInterestRateById
                    },
                    ControllerType.Currency => new[]
                    {
                        Permission.Currency.Create,
                        Permission.Currency.Update,
                        Permission.Currency.Delete,
                        Permission.Currency.ReadAll
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
                    _ => Array.Empty<string>()
                };

                result.Add(new RoleClaimsResDto
                {
                    Name = controller.ToString(),
                    Claims = permissions.Distinct().ToList()
                });
            }

            return result;
        }
    }
}
