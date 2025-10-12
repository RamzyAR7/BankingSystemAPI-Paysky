using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Identity
{
    /// <summary>
    /// Enforces username/email uniqueness scoped to BankId.
    /// </summary>
    public class BankScopedUserValidator : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (user == null) throw new ArgumentNullException(nameof(user));

            var errors = new List<IdentityError>();

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                errors.Add(new IdentityError { Code = "UserNameRequired", Description = "Username is required." });
                return IdentityResult.Failed(errors.ToArray());
            }

            var normalizedUserName = manager.NormalizeName(user.UserName ?? string.Empty);
            var normalizedEmail = manager.NormalizeEmail(user.Email ?? string.Empty);

            try
            {
                var existingByName = await manager.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.BankId == user.BankId && u.NormalizedUserName == normalizedUserName);

                if (existingByName != null && !string.Equals(existingByName.Id, user.Id, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new IdentityError { Code = "DuplicateUserName", Description = $"Username '{user.UserName}' is already taken." });
                }

                if (manager.Options.User.RequireUniqueEmail && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var existingByEmail = await manager.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.BankId == user.BankId && u.NormalizedEmail == normalizedEmail);

                    if (existingByEmail != null && !string.Equals(existingByEmail.Id, user.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new IdentityError { Code = "DuplicateEmail", Description = $"Email '{user.Email}' is already taken." });
                    }
                }
            }
            catch
            {
                // fallback sync enumeration
                foreach (var u in manager.Users)
                {
                    if (u.BankId == user.BankId && string.Equals(manager.NormalizeName(u.UserName ?? string.Empty), normalizedUserName, StringComparison.OrdinalIgnoreCase) && !string.Equals(u.Id, user.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new IdentityError { Code = "DuplicateUserName", Description = $"Username '{user.UserName}' is already taken." });
                        break;
                    }
                }

                if (manager.Options.User.RequireUniqueEmail && !string.IsNullOrWhiteSpace(user.Email))
                {
                    foreach (var u in manager.Users)
                    {
                        if (u.BankId == user.BankId && string.Equals(manager.NormalizeEmail(u.Email ?? string.Empty), normalizedEmail, StringComparison.OrdinalIgnoreCase) && !string.Equals(u.Id, user.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(new IdentityError { Code = "DuplicateEmail", Description = $"Email '{user.Email}' is already taken." });
                            break;
                        }
                    }
                }
            }

            return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
        }
    }
}
