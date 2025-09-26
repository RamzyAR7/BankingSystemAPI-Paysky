using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BankingSystemAPI.Infrastructure.Context;

namespace BankingSystemAPI.Infrastructure.Seeding
{
    public static class IdentitySeeding
    {
        public static async Task SeedingRoleAsync(RoleManager<ApplicationRole> _roleManager)
        {
            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = UserRole.SuperAdmin.ToString() });
                await _roleManager.CreateAsync(new ApplicationRole { Name = UserRole.Admin.ToString() });
                await _roleManager.CreateAsync(new ApplicationRole { Name = UserRole.Client.ToString() });
            }
        }

        public static async Task SeedingUsersAsync(
            UserManager<ApplicationUser> _userManager,
            RoleManager<ApplicationRole> _roleManager,
            ApplicationDbContext db)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = "SuperAdmin",
                FullName = "Super Admin",
                Email = "superadmin@paysky.io",
                NationalId = "12345678901234",
                PhoneNumber = "01027746531",
                DateOfBirth = new DateTime(2000, 1, 1),
            };

            var existingUser = await _userManager.FindByEmailAsync(superAdmin.Email);
            if (existingUser == null)
            {
                var result = await _userManager.CreateAsync(superAdmin, "SuperAdmin@123");
                if (result.Succeeded)
                {
                    await _userManager.SeedRoleTOUserAsync(superAdmin.Email, UserRole.SuperAdmin.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.User.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Role.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.RoleClaims.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.UserRoles.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Auth.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Account.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.CheckingAccount.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.SavingsAccount.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Currency.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Transaction.ToString());
                    await _roleManager.SeedClaimsToRoleAsync(UserRole.SuperAdmin.ToString(), ControllerType.Bank.ToString());
                }
            }

            // Seed admin and client claims
            var adminClaims = new[]
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
                Permission.User.UpdateActiveStatus,

                Permission.Auth.RevokeToken,

                Permission.Account.ReadById,
                Permission.Account.ReadByAccountNumber,
                Permission.Account.ReadByUserId,
                Permission.Account.ReadByNationalId,
                Permission.Account.Delete,
                Permission.Account.DeleteMany,
                Permission.Account.UpdateActiveStatus,

                Permission.CheckingAccount.Create,
                Permission.CheckingAccount.Update,
                Permission.CheckingAccount.ReadAll,
                Permission.CheckingAccount.UpdateActiveStatus,


                Permission.SavingsAccount.Create,
                Permission.SavingsAccount.Update,
                Permission.SavingsAccount.ReadAll,
                Permission.SavingsAccount.UpdateActiveStatus,
                Permission.SavingsAccount.ReadAllInterestRate,
                Permission.SavingsAccount.ReadInterestRateById,

                Permission.Currency.ReadAll,
                Permission.Currency.ReadById,

                Permission.Transaction.ReadBalance,
                Permission.Transaction.Deposit,
                Permission.Transaction.Withdraw,
                Permission.Transaction.Transfer,
                Permission.Transaction.ReadAllHistory,
                Permission.Transaction.ReadById
            };

            var clientClaims = new[]
            {
                Permission.Account.ReadById,
                Permission.Account.ReadByAccountNumber,
                Permission.Account.ReadByUserId,
                Permission.Transaction.ReadBalance,
                Permission.Transaction.Deposit,
                Permission.Transaction.Withdraw,
                Permission.Transaction.Transfer,
                Permission.Transaction.ReadById,
                Permission.Currency.ReadAll,
                Permission.User.ReadSelf,
                Permission.User.ChangePassword,
                Permission.SavingsAccount.ReadInterestRateById,
            };

            // Seed claims
            var adminRole = await _roleManager.FindByNameAsync(UserRole.Admin.ToString());
            if (adminRole != null)
            {
                var existingAdminClaims = await _roleManager.GetClaimsAsync(adminRole);
                foreach (var c in adminClaims)
                {
                    if (!existingAdminClaims.Any(x => x.Type == "Permission" && x.Value == c))
                    {
                        await _roleManager.AddClaimAsync(adminRole, new Claim("Permission", c));
                    }
                }
            }

            var clientRole = await _roleManager.FindByNameAsync(UserRole.Client.ToString());
            if (clientRole != null)
            {
                var existingClientClaims = await _roleManager.GetClaimsAsync(clientRole);
                foreach (var c in clientClaims)
                {
                    if (!existingClientClaims.Any(x => x.Type == "Permission" && x.Value == c))
                    {
                        await _roleManager.AddClaimAsync(clientRole, new Claim("Permission", c));
                    }
                }
            }
        }

        public static async Task SeedRoleTOUserAsync(this UserManager<ApplicationUser> _userManager, string userEmail, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }

        public static async Task SeedClaimsToRoleAsync(this RoleManager<ApplicationRole> _roleManager, string roleName, string controller)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            var roleClaims = await _roleManager.GetClaimsAsync(role);

            if (role != null)
            {
                IEnumerable<string> permissions = controller switch
                {
                    nameof(ControllerType.User) => new[]
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
                    nameof(ControllerType.UserRoles) => new[]
                    {
                        Permission.UserRoles.Assign
                    },
                    nameof(ControllerType.Role) => new[]
                    {
                        Permission.Role.Create,
                        Permission.Role.Delete,
                        Permission.Role.ReadAll
                    },
                    nameof(ControllerType.RoleClaims) => new[]
                    {
                        Permission.RoleClaims.Assign,
                        Permission.RoleClaims.ReadAll
                    },
                    nameof(ControllerType.Auth) => new[]
                    {
                        Permission.Auth.RevokeToken
                    },
                    nameof(ControllerType.Account) => new[]
                    {
                        Permission.Account.ReadById,
                        Permission.Account.ReadByAccountNumber,
                        Permission.Account.ReadByUserId,
                        Permission.Account.ReadByNationalId,
                        Permission.Account.Delete,
                        Permission.Account.DeleteMany,
                        Permission.Account.UpdateActiveStatus
                    },
                    nameof(ControllerType.CheckingAccount) => new[]
                    {
                        Permission.CheckingAccount.Create,
                        Permission.CheckingAccount.Update,
                        Permission.CheckingAccount.ReadAll,
                        Permission.CheckingAccount.UpdateActiveStatus
                    },
                    nameof(ControllerType.SavingsAccount) => new[]
                    {
                        Permission.SavingsAccount.Create,
                        Permission.SavingsAccount.Update,
                        Permission.SavingsAccount.ReadAll,
                        Permission.SavingsAccount.UpdateActiveStatus,
                        Permission.SavingsAccount.ReadAllInterestRate,
                        Permission.SavingsAccount.ReadInterestRateById
                    },
                    nameof(ControllerType.Currency) => new[]
                    {
                        Permission.Currency.Create,
                        Permission.Currency.Update,
                        Permission.Currency.Delete,
                        Permission.Currency.ReadAll,
                        Permission.Currency.ReadById
                    },
                    nameof(ControllerType.Transaction) => new[]
                    {
                        Permission.Transaction.ReadBalance,
                        Permission.Transaction.Deposit,
                        Permission.Transaction.Withdraw,
                        Permission.Transaction.Transfer,
                        Permission.Transaction.ReadAllHistory,
                        Permission.Transaction.ReadById
                    },
                    nameof(ControllerType.Bank) => new[]
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

                foreach (var permission in permissions)
                {
                    if (!roleClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                    {
                        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                    }
                }
            }
        }
    }
}
