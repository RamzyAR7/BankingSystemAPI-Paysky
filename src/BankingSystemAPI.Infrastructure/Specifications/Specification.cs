using BankingSystemAPI.Application.Interfaces.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BankingSystemAPI.Infrastructure.Specifications
{
    public class Specification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; private set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy { get; private set; }
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool AsNoTracking { get; private set; } = true;

        public Specification() { }
        public Specification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }
        public Specification<T> AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
            return this;
        }
        public Specification<T> ApplyOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        {
            OrderBy = orderBy;
            return this;
        }
        public Specification<T> ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            return this;
        }
        public Specification<T> AsNoTrackingQuery(bool asNoTracking = true)
        {
            AsNoTracking = asNoTracking;
            return this;
        }
    }
}
