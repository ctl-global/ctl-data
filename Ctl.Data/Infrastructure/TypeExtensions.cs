/*
    Copyright (c) 2016, CTL Global, Inc.
    All rights reserved.

    Redistribution and use in source and binary forms, with or without modification, are permitted
    provided that the following conditions are met:

    Redistributions of source code must retain the above copyright notice, this list of conditions
    and the following disclaimer. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the documentation and/or other
    materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
    IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
    FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
    THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
    OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    POSSIBILITY OF SUCH DAMAGE.
*/

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
