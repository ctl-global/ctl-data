using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace Ctl.Data.Infrastructure
{
    /// <summary>
    /// Extension methods to make working with types in .NET Standard easier.
    /// </summary>
    static class TypeExtensions
    {
#if !NET45
        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType);
        }

        public static TAttribute GetCustomAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute<TAttribute>();
        }
#endif

        public static ConstructorInfo GetConstructor(this Type type, BindingFlags flags, Type[] parameterTypes)
        {
#if NET45
            return type.GetConstructor(flags, null, parameterTypes, null);
#else
            foreach (var ctor in type.GetConstructors(flags))
            {
                ParameterInfo[] p = ctor.GetParameters();

                if (p?.Length != parameterTypes?.Length)
                {
                    continue;
                }

                for (int i = 0, len = parameterTypes?.Length ?? 0; i < len; ++i)
                {
                    if (p[i].ParameterType != parameterTypes[i]) continue;
                }

                return ctor;
            }

            return null;
#endif
        }
    }
}
