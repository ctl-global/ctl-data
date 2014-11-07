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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data.Infrastructure
{
    /// <summary>
    /// Caches dynamic methods and other expensive information used for type serialization.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    public static class SerializedType<T>
    {
        static int? fixedWidth;
        public static int FixedWidth
        {
            get
            {
                if (fixedWidth == null) fixedWidth = SerializedType.GetFixedWidth(typeof(T));
                return fixedWidth.Value;
            }
        }

        static string[] headers;
        public static string[] Headers
        {
            get
            {
                if (headers == null) headers = SerializedType.GetHeaders(typeof(T));
                return headers;
            }
        }

        static FixedPosition[] positions;
        public static FixedPosition[] Positions
        {
            get
            {
                if (positions == null) positions = SerializedType.GetPositions(typeof(T));
                return positions;
            }
        }

        static DeserializeFunc<T> readFunc;
        public static DeserializeFunc<T> ReadFunc
        {
            get
            {
                if (readFunc == null) readFunc = SerializedType.CompileReadFunc<T>(false);
                return readFunc;
            }
        }

        static DeserializeFunc<T> validatingReadFunc;
        public static DeserializeFunc<T> ValidatingReadFunc
        {
            get
            {
                if (validatingReadFunc == null) validatingReadFunc = SerializedType.CompileReadFunc<T>(true);
                return validatingReadFunc;
            }
        }

        static SerializeFunc<T> writeFunc;
        public static SerializeFunc<T> WriteFunc
        {
            get
            {
                if (writeFunc == null) writeFunc = SerializedType.CompileWriteFunc<T>(false);
                return writeFunc;
            }
        }

        static SerializeFunc<T> validatingWriteFunc;
        public static SerializeFunc<T> ValidatingWriteFunc
        {
            get
            {
                if (validatingWriteFunc == null) validatingWriteFunc = SerializedType.CompileWriteFunc<T>(true);
                return validatingWriteFunc;
            }
        }

        static Dictionary<string, int> readMap;
        public static Dictionary<string, int> ReadMap
        {
            get
            {
                if (readMap == null)
                {
                    SerializedMember[] cols = SerializedType.GetColumns(typeof(T)).ToArray();

                    Dictionary<string, int> newMap = new Dictionary<string, int>(cols.Length, StringComparer.InvariantCultureIgnoreCase);

                    for (int i = 0; i < cols.Length; ++i)
                    {
                        string[] names = cols[i].Names;

                        for (int j = 0; j < names.Length; ++j)
                        {
                            newMap[names[j]] = i;
                        }
                    }

                    readMap = newMap;
                }

                return readMap;
            }
        }

        static CsvHeaderIndex[] headerlessIndexes;

        public static CsvHeaderIndex[] HeaderlessIndexes
        {
            get
            {
                if(headerlessIndexes == null)
                {
                    var cols = SerializedType.GetColumns(typeof(T)).ToArray();
                    CsvHeaderIndex[] indexes = new CsvHeaderIndex[cols.Length];

                    for (int i = 0; i < cols.Length; ++i)
                    {
                        if (cols[i].IndexedPosition == -1)
                        {
                            throw new ParseException("Unable to deserialize CSV; columns must have explicit indexes.");
                        }

                        indexes[i].MemberIndex = i;
                        indexes[i].SerializedIndex = cols[i].IndexedPosition;
                    }

                    headerlessIndexes = indexes;
                }

                return headerlessIndexes;
            }
        }

        public static CsvHeaderIndex[] GetHeaderIndexes(IEnumerable<string> headers)
        {
            Debug.Assert(headers != null);

            return headers.Select((x, idx) => new CsvHeaderIndex
            {
                SerializedIndex = idx,
                MemberIndex = x != null && ReadMap.ContainsKey(x) ? ReadMap[x] : -1
            })
            .Where(x => x.MemberIndex != -1)
            .ToArray();
        }

        public static CsvHeaderIndex[] GetHeaderIndexes(IEnumerable<ColumnValue> headers)
        {
            Debug.Assert(headers != null);

            return headers.Select((x, idx) => new CsvHeaderIndex
            {
                SerializedIndex = idx,
                MemberIndex = x != null && x.Value != null && ReadMap.ContainsKey(x.Value) ? ReadMap[x.Value] : -1
            })
            .Where(x => x.MemberIndex != -1)
            .ToArray();
        }
    }

    /// <summary>
    /// Compiled serialization method generators.
    /// </summary>
    static partial class SerializedType
    {
        public static string[] GetHeaders(Type type)
        {
            return GetColumns(type).Select(x => x.Names.First()).ToArray();
        }

        public static FixedPosition[] GetPositions(Type type)
        {
            return GetColumns(type).Select(x => new FixedPosition(x.FixedPosition, x.FixedWidth)).ToArray();
        }

        public static IEnumerable<SerializedMember> GetColumns(Type t)
        {
            Debug.Assert(t != null);

            var members = from x in t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                          let pi = x as PropertyInfo
                          where (x is FieldInfo || (pi != null && pi.GetGetMethod() != null)) && x.GetCustomAttribute<NotMappedAttribute>() == null
                          let col = x.GetCustomAttribute<ColumnAttribute>()
                          let names = x.GetCustomAttributes<AdditionalNamesAttribute>()
                          let fmt = x.GetCustomAttribute<DataFormatAttribute>()
                          let width = x.GetCustomAttribute<FixedAttribute>()
                          let strlen = x.GetCustomAttribute<StringLengthAttribute>()
                          let maxlen = x.GetCustomAttribute<MaxLengthAttribute>()
                          let idx = x.GetCustomAttribute<PositionAttribute>()
                          let ret = new SerializedMember
                          {
                              MemberInfo = x,
                              Names = new[]
                               {
                                   new[] { col != null && !string.IsNullOrEmpty(col.Name) ? col.Name : x.Name },
                                   names.SelectMany(a => a.Names)
                               }.SelectMany(arr => arr).ToArray(),
                              Order = col != null ? col.Order : 0,
                              IndexedPosition = idx != null ? idx.ColumnIndex : -1,
                              Format = fmt != null ? fmt.Format : null,
                              NumberStyles = fmt != null ? fmt.NumberStyles : null,
                              DateTimeStyles = fmt != null ? fmt.DateTimeStyles : null,
                              FixedPosition = width != null ? width.Position : -1,
                              FixedWidth = width != null ? width.Width :
                                      strlen != null && strlen.MaximumLength > 0 ? strlen.MaximumLength :
                                      maxlen != null && maxlen.Length > 0 ? maxlen.Length :
                                      0,
                              IsReadOnly = (x is PropertyInfo && ((PropertyInfo)x).SetMethod == null),
                              IsRequired = x.GetCustomAttribute<RequiredAttribute>() != null
                          }
                          orderby ret.IndexedPosition, ret.FixedPosition, ret.Order, ret.Names.First()
                          select ret;

            int startIndexed = -1;
            int fixedPos = 0;

            foreach (SerializedMember m in members)
            {
                if (m.IndexedPosition == -1)
                {
                    m.IndexedPosition = ++startIndexed;
                }
                else
                {
                    startIndexed = m.IndexedPosition;
                }

                if (m.FixedPosition == -1)
                {
                    m.FixedPosition = fixedPos;
                }

                fixedPos += m.FixedWidth;

                yield return m;
            }
        }

        public static bool CanValidate(Type type)
        {
            Debug.Assert(type != null);

            return
                typeof(IValidatableObject).IsAssignableFrom(type)
                ||
                type.GetCustomAttributes(typeof(ValidationAttribute)).Any()
                ||
                type.GetMembers()
                .Where(x => x is FieldInfo || x is PropertyInfo)
                .SelectMany(x => x.GetCustomAttributes(typeof(ValidationAttribute)).Cast<ValidationAttribute>())
                .Any();
        }

        public static Expression Validate(Type type, Expression obj, Expression errors)
        {
            Debug.Assert(type != null);
            Debug.Assert(obj != null);
            Debug.Assert(errors != null && errors.Type == typeof(List<ValidationResult>));

            if (!CanValidate(type))
            {
                return Expression.Empty();
            }

            var validators = type.GetMembers()
                .Where(x => x is FieldInfo || x is PropertyInfo)
                .Select(x => new
                {
                    Member = x,
                    Attributes = x.GetCustomAttributes(typeof(ValidationAttribute)).Cast<ValidationAttribute>().ToArray()
                })
                .Where(x => x.Attributes.Length > 0)
                .ToArray();

            var typeValidators = type.GetCustomAttributes(typeof(ValidationAttribute)).OfType<ValidationAttribute>().ToArray();

            List<Expression> expressions = new List<Expression>();

            ParameterExpression ctx = Expression.Variable(typeof(ValidationContext), "ctx");
            expressions.Add(Expression.Assign(ctx, Expression.New(typeof(ValidationContext).GetConstructor(new[] { typeof(object) }), obj)));

            ParameterExpression res = null;
            ParameterExpression value = null;

            Expression hasNoErrors = Expression.Equal(Expression.Property(errors, "Count"), Expression.Constant(0));

            if (validators.Length != 0 || typeValidators.Length != 0)
            {
                res = Expression.Variable(typeof(ValidationResult), "res");
                value = Expression.Variable(typeof(object), "value");

                Expression isError = Expression.NotEqual(res, Expression.Field(null, typeof(ValidationResult).GetField("Success")));
                Expression addError = Expression.IfThen(isError, Expression.Call(errors, typeof(List<ValidationResult>).GetMethod("Add", new[] { typeof(ValidationResult) }), res));
                Expression displayName = Expression.Property(ctx, "DisplayName");
                Expression memberName = Expression.Property(ctx, "MemberName");
                MethodInfo getValidationResult = typeof(ValidationAttribute).GetMethod("GetValidationResult", new[] { typeof(object), typeof(ValidationContext) });

                foreach (var m in validators)
                {
                    DisplayNameAttribute dnattr = m.Member.GetCustomAttribute<DisplayNameAttribute>();

                    expressions.Add(Expression.Assign(memberName, Expression.Constant(m.Member.Name)));
                    expressions.Add(Expression.Assign(displayName, Expression.Constant(dnattr != null ? dnattr.DisplayName : m.Member.Name)));
                    expressions.Add(Expression.Assign(value, Expression.Convert(Expression.MakeMemberAccess(obj, m.Member), typeof(object))));

                    foreach (var v in m.Attributes)
                    {
                        Expression test = Expression.Call(Expression.Constant(v), getValidationResult, value, ctx);

                        expressions.Add(Expression.Assign(res, test));
                        expressions.Add(addError);
                    }
                }

                if (validators.Length != 0 && (typeValidators.Length != 0 || typeof(IValidatableObject).IsAssignableFrom(type)))
                {
                    // if has type validators or is IValidatable, then blank out the member/display names in ctx.

                    expressions.Add(Expression.Assign(memberName, Expression.Constant(null, typeof(string))));
                    expressions.Add(Expression.Assign(displayName, Expression.Constant(type.Name)));
                }

                if(typeValidators.Length != 0)
                {
                    List<Expression> subExpressions = new List<Expression>();

                    subExpressions.Add(Expression.Assign(value, Expression.Convert(obj, typeof(object))));

                    foreach (var v in typeValidators)
                    {
                        Expression test = Expression.Call(Expression.Constant(v), getValidationResult, value, ctx);

                        subExpressions.Add(Expression.Assign(res, test));
                        subExpressions.Add(addError);
                    }

                    expressions.Add(Expression.IfThen(hasNoErrors, Expression.Block(subExpressions)));
                }
            }

            if (typeof(IValidatableObject).IsAssignableFrom(type))
            {
                var selfErrors = Expression.Call(obj, typeof(IValidatableObject).GetMethod("Validate", new[] { typeof(ValidationContext) }), ctx);

                expressions.Add(Expression.IfThen(hasNoErrors, Expression.Call(errors, typeof(List<ValidationResult>).GetMethod("AddRange", new[] { typeof(IEnumerable<ValidationResult>) }), selfErrors)));
            }

            return Expression.Block(typeof(void), new[] { ctx, res, value }.Where(x => x != null), expressions);
        }

        public static int GetFixedWidth(Type type)
        {
            var a = type.GetCustomAttribute<FixedAttribute>();
            return a != null ? a.Width : -1;
        }
    }
}
