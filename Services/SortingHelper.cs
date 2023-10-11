using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProvidersDomain.Services
{
    public static class SortingHelper
    {
        public static IEnumerable<T> OrderByDynamic<T>(IEnumerable<T> items, string sortBy, string sortDirection)
        {
            try
            {
                var property = typeof(T).GetProperty(sortBy);

                var result = typeof(SortingHelper)
                    .GetMethod("OrderByDynamic_Private", BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(typeof(T), property.PropertyType)
                    .Invoke(null, new object[] { items, sortBy, sortDirection });

                return (IEnumerable<T>)result;
            }
            catch (Exception)
            {
                return items;
            }
        }

        private static IEnumerable<T> OrderByDynamic_Private<T, TKey>(IEnumerable<T> items, string sortBy, string sortDirection)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            Expression<Func<T, TKey>> property_access_expression =
                    Expression.Lambda<Func<T, TKey>>(Expression.Property(parameter, sortBy), parameter);

            

            if (sortDirection == "asc")
            {
                return items.OrderBy(property_access_expression.Compile());
            }

            if (sortDirection == "desc")
            {
                return items.OrderByDescending(property_access_expression.Compile());
            }

            throw new Exception("Invalid Sort Direction");
        }
    }
}
