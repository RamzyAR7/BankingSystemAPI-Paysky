using AutoMapper;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _db;
        public RoleService(RoleManager<ApplicationRole> roleManager, IMapper mapper, ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _mapper = mapper;
            _db = db;
        }

        public async Task<List<RoleResDto>> GetAllRolesAsync()
        {
            var roles = await Task.FromResult(_roleManager.Roles.ToList());
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

            return roleDtos;
        }

        public async Task<RoleUpdateResultDto> CreateRoleAsync(RoleReqDto dto)
        {
            var result = new RoleUpdateResultDto();
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                result.Errors.Add(new IdentityError { Description = "Role name cannot be null or empty." });
                result.Succeeded = false;
                return result;
            }

            var roleExists = await _roleManager.RoleExistsAsync(dto.Name);
            if (roleExists)
            {
                result.Errors.Add(new IdentityError { Description = $"Role '{dto.Name}' already exists." });
                result.Succeeded = false;
                return result;
            }

            var role = new ApplicationRole { Name = dto.Name };
            var identityResult = await _roleManager.CreateAsync(role);

            if (!identityResult.Succeeded)
            {
                result.Errors.AddRange(identityResult.Errors);
                result.Succeeded = false;
                return result;
            }

            result.Role = _mapper.Map<RoleResDto>(role);
            result.Succeeded = true;
            return result;
        }


        public async Task<RoleUpdateResultDto> DeleteRoleAsync(string roleId)
        {
            var result = new RoleUpdateResultDto();
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                result.Errors.Add(new IdentityError { Description = "Role not found." });
                result.Succeeded = false;
                return result;
            }

            // Prevent deletion if any users are assigned to this role (check Users.RoleId OR UserRoles join table)
            var hasUsersViaFk = await _db.Users.AnyAsync(u => u.RoleId == roleId);
            var hasUsersViaJoin = await _db.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
            if (hasUsersViaFk || hasUsersViaJoin)
            {
                result.Errors.Add(new IdentityError { Description = "Cannot delete role because it is assigned to one or more users." });
                result.Succeeded = false;
                return result;
            }

            var identityResult = await _roleManager.DeleteAsync(role);
            result.Succeeded = identityResult.Succeeded;
            result.Errors.AddRange(identityResult.Errors);
            return result;
        }
    }
}
