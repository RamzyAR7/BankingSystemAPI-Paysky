using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class RoleByIdSpecification : BaseSpecification<ApplicationRole>
    {
        public RoleByIdSpecification(string id) : base(r => r.Id == id) { }
    }
}
