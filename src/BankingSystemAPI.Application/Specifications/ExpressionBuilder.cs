using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BankingSystemAPI.Application.Specifications
{
    internal static class ExpressionBuilder
    {
        public static Func<IQueryable<T>, IOrderedQueryable<T>> BuildOrderBy<T>(string propertyName, bool descending)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("propertyName");

            // support nested properties via dot notation
            var param = Expression.Parameter(typeof(T), "x");
            Expression? body = param;
            Type currentType = typeof(T);

            foreach (var part in propertyName.Split('.'))
            {
                var prop = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                {
                    // fallback to object-based EF.Property approach
                    return q =>
                    {
                        if (descending)
                            return q.OrderByDescending(x => Microsoft.EntityFrameworkCore.EF.Property<object>(x, propertyName));
                        return q.OrderBy(x => Microsoft.EntityFrameworkCore.EF.Property<object>(x, propertyName));
                    };
                }

                body = Expression.Property(body!, prop);
                currentType = prop.PropertyType;
            }

            // key selector: x => x.Prop (Expression<Func<T, TProp>>)
            var keySelector = Expression.Lambda(body!, param);

            // choose method
            var methodName = descending ? "OrderByDescending" : "OrderBy";
            var queryableMethods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var method = queryableMethods
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(typeof(T), currentType);

            // build expression: q => q.OrderBy(x => x.Prop)
            var sourceParam = Expression.Parameter(typeof(IQueryable<T>), "q");
            var call = Expression.Call(method, sourceParam, Expression.Quote(keySelector));
            var lambda = Expression.Lambda<Func<IQueryable<T>, IOrderedQueryable<T>>>(call, sourceParam);
            return lambda.Compile();
        }
    }
}
