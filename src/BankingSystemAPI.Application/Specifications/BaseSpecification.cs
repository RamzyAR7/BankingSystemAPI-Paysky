#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public List<Expression<Func<T, object?>>> Includes { get; } = new();
        public Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy { get; protected set; }
        public int? Skip { get; protected set; }
        public int? Take { get; protected set; }
        public bool AsNoTracking { get; protected set; } = true;

        protected BaseSpecification() { }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        protected void AddInclude(Expression<Func<T, object?>> includeExpression)
            => Includes.Add(includeExpression);

        protected void ApplyOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderByExpression)
            => OrderBy = orderByExpression;

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        protected void DisableTracking()
            => AsNoTracking = false;

        public BaseSpecification<T> WithTracking(bool asNoTracking = true)
        {
            if (!asNoTracking)
                DisableTracking();
            return this;
        }
    }
}

