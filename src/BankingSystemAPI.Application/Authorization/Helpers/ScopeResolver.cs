using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public class ScopeResolver : IScopeResolver
    {
        private readonly ICurrentUserService _currentUser;

        public ScopeResolver(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task<AccessScope> GetScopeAsync()
        {
            var role = await _currentUser.GetRoleFromStoreAsync();
            if (RoleHelper.IsSuperAdmin(role.Name)) return AccessScope.Global;
            if (RoleHelper.IsClient(role.Name)) return AccessScope.Self;
            return AccessScope.BankLevel;
        }
    }
}
