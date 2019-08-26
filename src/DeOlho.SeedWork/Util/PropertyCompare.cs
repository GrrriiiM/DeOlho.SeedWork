using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DeOlho.SeedWork.Util
{
    public static class PropertyCompare 
    {
        public static bool Equal<T>(T x, T y, params string[] propertyNamesToIgnore) 
        {
            if (propertyNamesToIgnore != null)
            {
                if (!Cache.PropertyNamesToIgnore.ContainsKey(typeof(T)))
                {
                    Cache.PropertyNamesToIgnore.Add(typeof(T), propertyNamesToIgnore.ToList());
                }
            }
            return Cache<T>.Compare(x, y);
        }

        static class Cache
        {
            public static Dictionary<Type, List<string>> PropertyNamesToIgnore { get; set; } = new Dictionary<Type, List<string>>();
        }

        static class Cache<T> {
            internal static readonly Func<T, T, bool> Compare;
            
            static Cache() {
                var propsToIgnore = new List<string>();
                if (Cache.PropertyNamesToIgnore.ContainsKey(typeof(T)))
                {
                    propsToIgnore.AddRange(Cache.PropertyNamesToIgnore[typeof(T)]);
                }
                var props = typeof(T).GetProperties().Where(_ => !propsToIgnore.Contains(_.Name)).ToArray();
                if (props.Length == 0) {
                    Compare = delegate { return true; };
                    return;
                }
                var x = Expression.Parameter(typeof(T), "x");
                var y = Expression.Parameter(typeof(T), "y");

                Expression body = null;
                for (int i = 0; i < props.Length; i++) {
                    var propEqual = Expression.Equal(
                        Expression.Property(x, props[i]),
                        Expression.Property(y, props[i]));
                    if (body == null) {
                        body = propEqual;
                    } else {
                        body = Expression.AndAlso(body, propEqual);
                    }
                }
                Compare = Expression.Lambda<Func<T, T, bool>>(body, x, y)
                            .Compile();
            }
        }
    }
}