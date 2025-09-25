using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public static class RoleHelper
    {
        public static bool IsRole(this string? role, UserRole expectedRole)
            => string.Equals(role, expectedRole.ToString(), StringComparison.OrdinalIgnoreCase);

        public static bool IsSuperAdmin(this string? role)
            => role.IsRole(UserRole.SuperAdmin);

        public static bool IsClient(this string? role)
            => role.IsRole(UserRole.Client);

        public static bool IsBankAdmin(this string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;

            return !role.IsRole(UserRole.Client) && !role.IsRole(UserRole.SuperAdmin);
        }
    }
}
