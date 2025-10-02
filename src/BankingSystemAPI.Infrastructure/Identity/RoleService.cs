using AutoMapper;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMapper _mapper;

        public RoleService(RoleManager<ApplicationRole> roleManager, IMapper mapper)
        {
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public async Task<Result<List<RoleResDto>>> GetAllRolesAsync()
        {
            try
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

                return Result<List<RoleResDto>>.Success(roleDtos);
            }
            catch (System.Exception ex)
            {
                return Result<List<RoleResDto>>.Failure(new[] { $"Failed to retrieve roles: {ex.Message}" });
            }
        }

        public async Task<Result<RoleUpdateResultDto>> CreateRoleAsync(RoleReqDto dto)
        {
            // Input validation
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Role name cannot be null or empty." });
            }

            // Check if role already exists
            var existingRole = await _roleManager.FindByNameAsync(dto.Name);
            if (existingRole != null)
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { $"Role '{dto.Name}' already exists." });
            }

            var role = new ApplicationRole { Name = dto.Name };
            var identityResult = await _roleManager.CreateAsync(role);

            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<RoleUpdateResultDto>.Failure(errors);
            }

            var result = new RoleUpdateResultDto
            {
                Role = _mapper.Map<RoleResDto>(role),
                Succeeded = true,
                Errors = new List<IdentityError>()
            };

            return Result<RoleUpdateResultDto>.Success(result);
        }

        public async Task<Result<RoleUpdateResultDto>> DeleteRoleAsync(string roleId)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Role ID cannot be null or empty." });
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Role not found." });
            }

            var identityResult = await _roleManager.DeleteAsync(role);
            
            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors.Select(e => e.Description);
                return Result<RoleUpdateResultDto>.Failure(errors);
            }

            var result = new RoleUpdateResultDto
            {
                Role = _mapper.Map<RoleResDto>(role),
                Succeeded = true,
                Errors = new List<IdentityError>()
            };
            
            return Result<RoleUpdateResultDto>.Success(result);
        }
    }
}
