#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
#endregion


namespace BankingSystemAPI.Infrastructure.SpecificationEvaluatorClass
{
    internal static class SpecificationEvaluator
    {
        public static IQueryable<T> ApplySpecification<T>(IQueryable<T> query, ISpecification<T> spec, bool evaluatePaging = true) where T : class
        {
            if (spec == null) return query;

            if (spec.AsNoTracking)
                query = query.AsNoTracking();

            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            foreach (var include in spec.Includes ?? Enumerable.Empty<Expression<Func<T, object?>>>())
                query = query.Include(include);

            if (spec.OrderBy != null)
                query = spec.OrderBy(query);

            if (evaluatePaging)
            {
                if ((spec.Skip.HasValue || spec.Take.HasValue) && spec.OrderBy == null)
                {
                    query = query.OrderBy(x => EF.Property<object>(x, "Id"));
                }

                if (spec.Skip.HasValue)
                    query = query.Skip(spec.Skip.Value);
                if (spec.Take.HasValue)
                    query = query.Take(spec.Take.Value);
            }

            return query;
        }
    }
}

