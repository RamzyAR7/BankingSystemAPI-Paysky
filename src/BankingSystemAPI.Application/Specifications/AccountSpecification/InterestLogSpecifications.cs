using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
using System.Linq;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class InterestLogsPagedSpecification : BaseSpecification<InterestLog>
    {
        public InterestLogsPagedSpecification(int skip, int take)
            : base(l => true)
        {
            ApplyPaging(skip, take);
            // Default ordering: newest first
            ApplyOrderBy(q => q.OrderByDescending(l => l.Timestamp));
            // include related savings account if needed
            AddInclude(l => l.SavingsAccount);
        }
    }

    public class InterestLogsByAccountPagedSpecification : BaseSpecification<InterestLog>
    {
        public InterestLogsByAccountPagedSpecification(int accountId, int skip, int take)
            : base(l => l.SavingsAccountId == accountId)
        {
            ApplyPaging(skip, take);
            ApplyOrderBy(q => q.OrderByDescending(l => l.Timestamp));
            AddInclude(l => l.SavingsAccount);
        }
    }
}
