using AutoMapper;
using BankingSystemAPI.Application.DTOs.Role;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Infrastructure.Mapping
{
    public class IdentityMappingProfile:Profile
    {
        public IdentityMappingProfile()
        {
            #region Role
            CreateMap<ApplicationRole, RoleResDto>();
            CreateMap<RoleReqDto, ApplicationRole>();
            #endregion
        }
    }
}
