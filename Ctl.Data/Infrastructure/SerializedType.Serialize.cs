/*
    Copyright (c) 2014, CTL Global, Inc.
    Copyright (c) 2012, iD Commerce + Logistics
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data.Infrastructure
{
    public delegate void SerializeFunc<T>(string[] valuesArray, T obj, IFormatProvider fmtProvider, List<ValidationResult> validationResults);

    static partial class SerializedType
    {
        /// <summary>
        /// Builds a function which serializes an object into a string array.
        /// </summary>
        public static SerializeFunc<T> CompileWriteFunc<T>(bool validate)
        {
            validate = validate && CanValidate(typeof(T));

            var valuesArray = Expression.Parameter(typeof(string[]), "valuesArray");
            var obj = Expression.Parameter(typeof(T), "obj");
            var fmtProvider = Expression.Parameter(typeof(IFormatProvider), "fmtProvider");
            var validationResults = Expression.Parameter(typeof(List<ValidationResult>), "validationResults");

            var lambda = Expression.Lambda<SerializeFunc<T>>(
                SerializeObject(valuesArray, obj, fmtProvider, validationResults, validate),
                "SerializeRecord",
                new[] { valuesArray, obj, fmtProvider, validationResults });

            return lambda.Compile();
        }

        static Expression SerializeObject(Expression valuesArray, Expression obj, Expression fmtProvider, Expression validationResults, bool validate)
        {
            Debug.Assert(!validate || validationResults != null);

            List<Expression> statements = new List<Expression>();

            if (validate)
            {
                statements.Add(Validate(obj.Type, obj, validationResults));
                statements.Add(Expression.IfThen(
                    Expression.NotEqual(Expression.Property(validationResults, "Count"), Expression.Constant(0)),
                    Expression.Throw(Expression.New(
                        typeof(ValidationException).GetConstructor(new[] { typeof(IEnumerable<ValidationResult>), typeof(object), typeof(long), typeof(long) }),
                        validationResults, obj, Expression.Constant(0L), Expression.Constant(0L)))));
            }

            statements.AddRange(GetColumns(obj.Type).Select((m, idx) => Expression.Assign(
                    Expression.ArrayAccess(valuesArray, Expression.Constant(idx)),
                    SerializeMemberValue(Expression.MakeMemberAccess(obj, m.MemberInfo), fmtProvider, m.Format))));

            return Expression.Block(typeof(void), statements);
        }

        static Expression SerializeMemberValue(Expression srcObject, Expression fmtProvider, string format)
        {
            Debug.Assert(srcObject != null);
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));

            if (srcObject.Type == typeof(string))
            {
                return srcObject;
            }

            Expression nullString = Expression.Constant(null, typeof(string));

            // for a reference type.

            if (srcObject.Type.IsClass)
            {
                return Expression.Condition(
                    Expression.NotEqual(srcObject, Expression.Constant(null, srcObject.Type)),
                    CallToString(srcObject, fmtProvider, format),
                    nullString);
            }

            // for a Nullable<> type.

            Type innerSrcType = Nullable.GetUnderlyingType(srcObject.Type);

            if (innerSrcType != null)
            {
                return Expression.Condition(
                    Expression.Property(srcObject, "HasValue"),
                    CallToString(Expression.Property(srcObject, "Value"), fmtProvider, format),
                    nullString);
            }

            // for a value type.
            return CallToString(srcObject, fmtProvider, format);
        }

        /// <summary>
        /// Calls the best available ToString() method for the type.
        /// </summary>
        static Expression CallToString(Expression srcObject, Expression fmtProvider, string format)
        {
            Debug.Assert(srcObject != null);
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));

            if (srcObject.Type == typeof(string))
            {
                return srcObject;
            }

            MethodInfo method;
            if (typeof(IFormattable).IsAssignableFrom(srcObject.Type))
            {
                method = typeof(IFormattable).GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
                return Expression.Call(srcObject, method, Expression.Constant(format, typeof(string)), fmtProvider);
            }

            if(!string.IsNullOrEmpty(format))
            {
                method = srcObject.Type.GetMethod("ToString", new[] { typeof(string) });
                if (method != null)
                {
                    return Expression.Call(srcObject, method, Expression.Constant(format, typeof(string)));
                }
            }

            method = srcObject.Type.GetMethod("ToString", new[] { typeof(IFormatProvider) });
            if (method != null)
            {
                return Expression.Call(srcObject, method, fmtProvider);
            }

            method = srcObject.Type.GetMethod("ToString", Type.EmptyTypes);
            return Expression.Call(srcObject, method);
        }
    }
}
