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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Ctl.Data.Infrastructure;

namespace Ctl.Data
{
    /// <summary>
    /// An object reader that operates on headered data.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    public class HeaderedObjectReader<T> : ObjectReader<T>, IDataReader<T>
    {
        readonly IEqualityComparer<string> headerComparer;
        readonly IDataReader reader;
        readonly bool validate;

        /// <summary>
        /// Initializes a new HeaderedObjectReader.
        /// </summary>
        /// <param name="reader">The IDataReader to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="readHeader">If true, read a header. Otherwise, use column indexes.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        public HeaderedObjectReader(IDataReader reader, IFormatProvider formatProvider, bool validate, bool readHeader)
            : this(reader, formatProvider, validate, readHeader, null)
        {
        }

        /// <summary>
        /// Initializes a new HeaderedObjectReader.
        /// </summary>
        /// <param name="reader">The IDataReader to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="readHeader">If true, read a header. Otherwise, use column indexes.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        /// <param name="headerComparer">Used when comparing header values to property names. If null, the InvariantCultureIgnoreCase comparer will be used.</param>
        public HeaderedObjectReader(IDataReader reader, IFormatProvider formatProvider, bool validate, bool readHeader, IEqualityComparer<string> headerComparer)
            : base(formatProvider, validate)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            this.reader = reader;
            this.validate = validate;

            if (!readHeader)
            {
                headers = SerializedType<T>.HeaderlessIndexes;
            }

            this.headerComparer = headerComparer;
        }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false if the end of the TextReader has been reached.</returns>
        public bool Read()
        {
            if (!reader.Read())
            {
                return false;
            }

            if (headers == null)
            {
                InitHeaders();

                if (!reader.Read())
                {
                    return false;
                }
            }

            Deserialize(reader.CurrentRow);
            return true;
        }

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        public bool TryRead()
        {
            if (!reader.TryRead())
            {
                return false;
            }

            if (headers == null)
            {
                InitHeaders();

                if (!reader.TryRead())
                {
                    return false;
                }
            }

            Deserialize(reader.CurrentRow);
            return true;
        }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>If a record was read, true. Otherwise, false if the end of the TextReader has been reached.</returns>
        public Task<bool> ReadAsync(CancellationToken token)
        {
            return TryRead() ? Constants.TrueTask : ReadAsyncImpl(token);
        }

        async Task<bool> ReadAsyncImpl(CancellationToken token)
        {
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return false;
            }

            if (headers == null)
            {
                InitHeaders();

                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Deserialize(reader.CurrentRow);
            return true;
        }

        void InitHeaders()
        {
            headers = SerializedType<T>.GetHeaderIndexes(reader.CurrentRow, headerComparer);

            if (!validate) return;

            // validate any non-nullable members marked with [Required] have a header present,
            // because [Required] won't detect default(T) of the member as a missing value.

            List<ValidationResult> vrs = new List<ValidationResult>();

            foreach (var c in SerializedType.GetColumns(typeof(T)))
            {
                if (!c.IsRequired)
                {
                    continue;
                }

                FieldInfo fi = c.MemberInfo as FieldInfo;
                PropertyInfo pi = c.MemberInfo as PropertyInfo;
                Type t = fi != null ? fi.FieldType : pi.PropertyType;

                if (t.GetTypeInfo().IsClass || Nullable.GetUnderlyingType(t) != null || c.Names.Intersect(reader.CurrentRow.Select(x => x.Value), headerComparer ?? StringComparer.OrdinalIgnoreCase).Any())
                {
                    continue;
                }

                vrs.Add(new ValidationResult(c.MemberInfo.GetCustomAttribute<RequiredAttribute>().FormatErrorMessage(c.MemberInfo.Name), new[] { c.MemberInfo.Name }));
            }

            if (vrs.Count != 0)
            {
                base.missingHeaderErrors = vrs;
            }
        }
    }
}
