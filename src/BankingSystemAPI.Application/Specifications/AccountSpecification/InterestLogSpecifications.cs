#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class InterestLogsPagedSpecification : BaseSpecification<InterestLog>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public InterestLogsPagedSpecification(int skip, int take)
            : base(l => true)
        {
            ApplyPaging(skip, take);
            // Default ordering: newest first
            ApplyOrderBy(q => q.OrderByDescending(l => l.Timestamp));
            // include related savings account if needed
            AddInclude(l => l.SavingsAccount);
        }

        public InterestLogsPagedSpecification(IEnumerable<int> accountIds, int skip, int take)
            : base(l => accountIds != null && accountIds.Contains(l.SavingsAccountId))
        {
            ApplyPaging(skip, take);
            ApplyOrderBy(q => q.OrderByDescending(l => l.Timestamp));
            AddInclude(l => l.SavingsAccount);
        }
    }

    public class InterestLogsByAccountPagedSpecification : BaseSpecification<InterestLog>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public InterestLogsByAccountPagedSpecification(int accountId, int skip, int take)
            : base(l => l.SavingsAccountId == accountId)
        {
            ApplyPaging(skip, take);
            ApplyOrderBy(q => q.OrderByDescending(l => l.Timestamp));
            AddInclude(l => l.SavingsAccount);
        }
    }
}

