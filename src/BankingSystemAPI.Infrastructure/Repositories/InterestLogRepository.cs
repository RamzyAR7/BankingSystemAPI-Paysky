using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class InterestLogRepository : GenericRepository<InterestLog, int>, IInterestLogRepository
    {
        public InterestLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
