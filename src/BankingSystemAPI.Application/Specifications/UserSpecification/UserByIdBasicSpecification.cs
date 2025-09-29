using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class UserByIdBasicSpecification : BaseSpecification<ApplicationUser>
    {
        public UserByIdBasicSpecification(string id) : base(u => u.Id == id)
        {
            // no includes
        }
    }
}
