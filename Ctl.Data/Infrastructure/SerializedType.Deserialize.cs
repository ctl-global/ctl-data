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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data.Infrastructure
{
    public delegate ObjectValue<T> DeserializeFunc<T>(CsvHeaderIndex[] headersArray, RowValue valuesList, IFormatProvider fmtProvider, List<Exception> exceptions, List<ValidationResult> validationResults);

    static partial class SerializedType
    {
        public static DeserializeFunc<T> CompileReadFunc<T>(bool validate)
        {
            var headersArray = Expression.Parameter(typeof(CsvHeaderIndex[]), "headersArray");
            var valuesList = Expression.Parameter(typeof(RowValue), "valuesList");
            var fmtProvider = Expression.Parameter(typeof(IFormatProvider), "fmtProvider");
            var exceptions = Expression.Parameter(typeof(List<Exception>), "exceptions");
            var validationResults = Expression.Parameter(typeof(List<ValidationResult>), "validationResults");

            var lambda = Expression.Lambda<DeserializeFunc<T>>(
                ParseObject(typeof(T), headersArray, valuesList, fmtProvider, exceptions, validationResults, validate),
                "DeserializeRecord",
                new[] { headersArray, valuesList, fmtProvider, exceptions, validationResults });

            return lambda.Compile();
        }

        /// <summary>
        /// Parses and returns a full object
        /// </summary>
        static Expression ParseObject(Type type, Expression headersArray, Expression valuesList, Expression fmtProvider, Expression exceptions, Expression errors, bool validate)
        {
            Debug.Assert(type != null);
            Debug.Assert(headersArray != null && headersArray.Type == typeof(CsvHeaderIndex[]));
            Debug.Assert(valuesList != null && valuesList.Type == typeof(RowValue));
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));
            Debug.Assert(exceptions != null && exceptions.Type == typeof(List<Exception>));
            Debug.Assert(errors != null && errors.Type == typeof(List<ValidationResult>));

            var members = GetColumns(type).Where(x => !x.IsReadOnly);

            Type returnType = typeof(ObjectValue<>).MakeGenericType(type);

            ParameterExpression cv = Expression.Variable(typeof(ColumnValue), "cv");
            ParameterExpression obj = Expression.Variable(type, "obj");
            ParameterExpression i = Expression.Variable(typeof(int), "i");
            ParameterExpression headersCount = Expression.Variable(typeof(int), "headersCount");
            ParameterExpression valuesCount = Expression.Variable(typeof(int), "valuesCount");
            ParameterExpression sourceIdx = Expression.Variable(typeof(int), "sourceIdx");
            ParameterExpression str = Expression.Variable(typeof(string), "str");

            LabelTarget returnTarget = Expression.Label(returnType);
            LabelTarget breakTarget = Expression.Label();

            var cvLineNumber = Expression.Field(cv, "lineNumber");
            var cvColumnNumber = Expression.Field(cv, "columnNumber");

            List<Expression> statements = new List<Expression>
            {
                Expression.Assign(obj, Expression.New(type)),
                Expression.Assign(headersCount, Expression.Property(headersArray, "Length")),
                Expression.Assign(valuesCount, Expression.Property(valuesList, typeof(ICollection<ColumnValue>).GetProperty("Count"))),
                Expression.Assign(i, Expression.Constant(0)),
                Expression.Loop(Expression.Block(
                    Expression.IfThen(Expression.Equal(i, headersCount), Expression.Break(breakTarget)),
                    Expression.Assign(sourceIdx, Expression.Field(Expression.ArrayIndex(headersArray, i), "SerializedIndex")),
                    Expression.IfThen(Expression.GreaterThanOrEqual(sourceIdx, valuesCount), Expression.Break(breakTarget)),
                    Expression.Assign(cv, Expression.Call(valuesList, typeof(IList<ColumnValue>).GetMethod("get_Item", new[] { typeof(int) }), sourceIdx)),
                    Expression.Assign(str, Expression.Field(cv, "value")),
                    Expression.Switch(
                        Expression.Field(Expression.ArrayIndex(headersArray, i), "MemberIndex"),
                        members
                            .Select((m, idx) => Expression.SwitchCase(ParseMember(
                                m, obj,
                                str,
                                fmtProvider,
                                exceptions,
                                cvLineNumber,
                                cvColumnNumber
                                ), Expression.Constant(idx)))
                            .ToArray()
                    ),
                    Expression.PreIncrementAssign(i)
                ), breakTarget)
            };

            statements.Add(Expression.IfThen(Expression.NotEqual(Expression.Property(exceptions, "Count"), Expression.Constant(0)),
                Expression.Return(returnTarget, Expression.New(returnType.GetConstructor(new[] { typeof(RowValue), typeof(IEnumerable<Exception>) }), valuesList, exceptions))
            ));

            if (validate && SerializedType.CanValidate(type))
            {
                statements.Add(SerializedType.Validate(type, obj, errors));
                statements.Add(Expression.IfThen(Expression.NotEqual(Expression.Property(errors, "Count"), Expression.Constant(0)),
                    Expression.Return(returnTarget, Expression.New(returnType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(RowValue), typeof(IEnumerable<ValidationResult>), type }, null), valuesList, errors, obj))
                ));
            }

            statements.Add(Expression.Label(returnTarget, Expression.New(returnType.GetConstructor(new[] { typeof(RowValue), type }), valuesList, obj)));

            return Expression.Block(new[] { cv, obj, i, headersCount, valuesCount, sourceIdx, str }, statements);
        }

        /// <summary>
        /// Parses a specific member, catching any exceptions.
        /// </summary>
        static Expression ParseMember(SerializedMember member, Expression dstObj, Expression srcString, Expression fmtProvider, Expression exceptions, Expression lineNo, Expression colNo)
        {
            Debug.Assert(dstObj != null);
            Debug.Assert(srcString != null && srcString.Type == typeof(string));
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));
            Debug.Assert(exceptions != null && exceptions.Type == typeof(List<Exception>));
            Debug.Assert(lineNo != null && lineNo.Type == typeof(long));
            Debug.Assert(colNo != null && colNo.Type == typeof(long));

            ParameterExpression ex = Expression.Parameter(typeof(Exception));
            Expression dstValue = Expression.MakeMemberAccess(dstObj, member.MemberInfo);

            return Expression.TryCatch(
                ParseMemberValue(dstValue, srcString, fmtProvider, member),
                Expression.Catch(ex, Expression.Call(exceptions, typeof(List<Exception>).GetMethod("Add", new[] { typeof(Exception) }), Expression.New(
                        typeof(SerializationException).GetConstructor(new[] { typeof(long), typeof(long), typeof(string), typeof(string), typeof(Exception) }),
                        lineNo,
                        colNo,
                        Expression.Constant(member.Names.First()),
                        srcString,
                        ex))));
        }

        /// <summary>
        /// Parses depending on type member's type -- string, class, Nullable, or struct.
        /// </summary>
        static Expression ParseMemberValue(Expression dstValue, Expression srcString, Expression fmtProvider, SerializedMember member)
        {
            Debug.Assert(dstValue != null);
            Debug.Assert(srcString != null && srcString.Type == typeof(string));
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));

            Type dstType = dstValue.Type;
            Type innerDstType = Nullable.GetUnderlyingType(dstType);

            Expression parse;

            if (dstType == typeof(string))
            {
                // Parsing a string -- just assign it directly.

                parse = srcString;
            }
            else if (dstType.IsClass)
            {
                // Parsing a reference type.

                parse = Expression.Condition(Expression.Call(typeof(string).GetMethod("IsNullOrEmpty"), srcString),
                    Expression.Default(dstType),
                    CallParse(dstType, srcString, fmtProvider, member));
            }
            else if (innerDstType != null)
            {
                // Parsing a Nullable<> type.

                parse = Expression.Condition(Expression.Call(typeof(string).GetMethod("IsNullOrEmpty"), srcString),
                    Expression.Default(dstType),
                    Expression.New(dstType.GetConstructor(new[] { innerDstType }), CallParse(innerDstType, srcString, fmtProvider, member)));
            }
            else
            {
                // Parsing a value type.

                parse = CallParse(dstType, srcString, fmtProvider, member);
            }

            return Expression.Block(typeof(void), Expression.Assign(dstValue, parse));
        }

        /// <summary>
        /// Calls the best available Parse() method for the type.
        /// </summary>
        static Expression CallParse(Type dstType, Expression srcString, Expression fmtProvider, SerializedMember member)
        {
            Debug.Assert(dstType != null);
            Debug.Assert(srcString != null && srcString.Type == typeof(string));
            Debug.Assert(fmtProvider != null && fmtProvider.Type == typeof(IFormatProvider));

            if (dstType == typeof(string))
            {
                return srcString;
            }

            MethodInfo method;

            if (dstType.IsEnum)
            {
                method = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string) });
                return Expression.Convert(Expression.Call(method, Expression.Constant(dstType), srcString), dstType);
            }

            method = dstType.GetMethod("ParseExact", new[] { typeof(string), typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles) });

            if (member.Format != null && member.DateTimeStyles != null && method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString, Expression.Constant(member.Format), fmtProvider, Expression.Constant(member.NumberStyles));
            }

            method = dstType.GetMethod("ParseExact", new[] { typeof(string), typeof(string), typeof(IFormatProvider) });

            if (member.Format != null && method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString, Expression.Constant(member.Format), fmtProvider);
            }

            method = dstType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles) });

            if (member.DateTimeStyles != null && method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString, fmtProvider, Expression.Constant(member.DateTimeStyles));
            }

            method = dstType.GetMethod("Parse", new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider) });

            if (member.NumberStyles != null && method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString, Expression.Constant(member.NumberStyles), fmtProvider);
            }

            method = dstType.GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });

            if (method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString, fmtProvider);
            }

            method = dstType.GetMethod("Parse", new[] { typeof(string) });

            if (method != null && method.ReturnType == dstType)
            {
                return Expression.Call(method, srcString);
            }

            throw new ArgumentException("Unable to find viable parse method for type " + dstType.Name + ".", "dstType");
        }
    }
}
