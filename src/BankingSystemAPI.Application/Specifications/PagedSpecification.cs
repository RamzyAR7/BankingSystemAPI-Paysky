#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications
{
    /// <summary>
    /// Generic paged specification that supports dynamic ordering (by property name) and includes.
    /// Use this to avoid repeating paging + ordering logic across specific specs.
    /// </summary>
    public class PagedSpecification<T> : BaseSpecification<T>
    {
        public PagedSpecification(Expression<Func<T, bool>> criteria, int skip, int take, string? orderByProperty = null, string? orderDirection = null, params Expression<Func<T, object?>>[] includes)
            : base(criteria ?? (x => true))
        {
            Skip = skip;
            Take = take;

            if (includes != null)
            {
                foreach (var inc in includes)
                    AddInclude(inc);
            }

            if (!string.IsNullOrWhiteSpace(orderByProperty))
            {
                var dir = (orderDirection ?? "ASC").ToUpperInvariant();
                var descending = dir != "ASC";

                ApplyOrderBy(q =>
                {
                    if (descending)
                        return q.OrderByDescending(x => EF.Property<object>(x, orderByProperty));
                    return q.OrderBy(x => EF.Property<object>(x, orderByProperty));
                });
            }
        }

        public PagedSpecification(int skip, int take, string? orderByProperty = null, string? orderDirection = null, params Expression<Func<T, object?>>[] includes)
            : this(null, skip, take, orderByProperty, orderDirection, includes)
        {
        }
    }
}

