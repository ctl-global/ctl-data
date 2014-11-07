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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// A streaming reader.
    /// </summary>
    public interface IStreamingReader
    {
        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false if the end of the TextReader has been reached.</returns>
        bool Read();

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>If a record was read, true. Otherwise, false if the end of the TextReader has been reached.</returns>
        Task<bool> ReadAsync(CancellationToken token);

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        bool TryRead();
    }

    /// <summary>
    /// A data row reader.
    /// </summary>
    public interface IDataReader : IStreamingReader
    {
        /// <summary>
        /// The record read.
        /// </summary>
        RowValue CurrentRow { get; }
    }

    /// <summary>
    /// An object reader.
    /// </summary>
    /// <typeparam name="T">The type of object to read.</typeparam>
    public interface IDataReader<T> : IStreamingReader
    {
        /// <summary>
        /// The object read.
        /// </summary>
        ObjectValue<T> CurrentObject { get; }
    }

    /// <summary>
    /// Extensions for the IDataReader interface.
    /// </summary>
    public static class StreamingReaderExtensions
    {
        /// <summary>
        /// Reads the next record from a streaming reader.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>If a record was read, true. Otherwise, false if the end of the input stream has been reached.</returns>
        public static Task<bool> ReadAsync(this IStreamingReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return reader.ReadAsync(CancellationToken.None);
        }

        /// <summary>
        /// Transforms this reader into an enumerable of rows.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The reader as an enumerable.</returns>
        public static IEnumerable<RowValue> AsEnumerable(this IDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return new DataReaderEnumerable(reader);
        }

        /// <summary>
        /// Transforms this reader into an asynchronous enumerable of rows.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The reader as an asynchronous enumerable.</returns>
        public static IAsyncEnumerable<RowValue> AsAsyncEnumerable(this IDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return new DataReaderEnumerable(reader);
        }

        /// <summary>
        /// Transforms this reader into an enumerable of objects.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The reader as an enumerable.</returns>
        public static IEnumerable<ObjectValue<T>> AsEnumerable<T>(this IDataReader<T> reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return new DataReaderEnumerable<T>(reader);
        }

        /// <summary>
        /// Transforms this reader into an asynchronous enumerable of objects.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>The reader as an enumerable.</returns>
        public static IAsyncEnumerable<ObjectValue<T>> AsAsyncEnumerable<T>(this IDataReader<T> reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return new DataReaderEnumerable<T>(reader);
        }

        sealed class DataReaderEnumerable : IEnumerable<RowValue>, IEnumerator<RowValue>, IAsyncEnumerable<RowValue>, IAsyncEnumerator<RowValue>
        {
            readonly IDataReader reader;

            public DataReaderEnumerable(IDataReader reader)
            {
                Debug.Assert(reader != null);
                this.reader = reader;
            }

            public IEnumerator<RowValue> GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }

            IAsyncEnumerator<RowValue> IAsyncEnumerable<RowValue>.GetEnumerator()
            {
                return this;
            }

            public RowValue Current
            {
                get { return reader.CurrentRow; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return reader.Read();
            }

            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                return reader.TryRead() ? Constants.TrueTask : reader.ReadAsync(cancellationToken);
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        sealed class DataReaderEnumerable<T> : IEnumerable<ObjectValue<T>>, IEnumerator<ObjectValue<T>>, IAsyncEnumerable<ObjectValue<T>>, IAsyncEnumerator<ObjectValue<T>>
        {
            readonly IDataReader<T> reader;

            public DataReaderEnumerable(IDataReader<T> reader)
            {
                Debug.Assert(reader != null);
                this.reader = reader;
            }

            public IEnumerator<ObjectValue<T>> GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }

            IAsyncEnumerator<ObjectValue<T>> IAsyncEnumerable<ObjectValue<T>>.GetEnumerator()
            {
                return this;
            }

            public ObjectValue<T> Current
            {
                get { return reader.CurrentObject; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return reader.Read();
            }

            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                return reader.ReadAsync(cancellationToken);
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Filters a sequence of ObjectValues into plain values.
        /// If errors are encountered, the sequence is allowed to continue
        /// materializing to capture all errors in the sequence, followed
        /// by throwing AggregateException containing all errors.
        /// </summary>
        /// <typeparam name="T">The type of sequence to enumerate.</typeparam>
        /// <param name="values">The sequence to enumerate.</param>
        /// <returns>A sequence of values.</returns>
        public static IEnumerable<T> WithMaterializedErrors<T>(this IEnumerable<ObjectValue<T>> values)
        {
            if(values == null) throw new ArgumentNullException("values");

            List<Exception> exceptions = new List<Exception>();

            foreach(var v in values)
            {
                if (v == null)
                {
                    throw new ArgumentNullException("values", "Sequence must not contain null ObjectValues.");
                }

                if(v.Exception == null)
                {
                    if(exceptions.Count == 0)
                    {
                        yield return v.Value;
                    }
                }
                else
                {
                    exceptions.Add(v.Exception);
                }
            }

            if(exceptions.Count != 0)
            {
                throw new AggregateException(exceptions).Flatten();
            }
        }

        /// <summary>
        /// Filters a sequence of ObjectValues into plain values.
        /// If errors are encountered, the sequence is allowed to continue
        /// materializing to capture all errors in the sequence, followed
        /// by throwing AggregateException containing all errors.
        /// </summary>
        /// <typeparam name="T">The type of sequence to enumerate.</typeparam>
        /// <param name="values">The sequence to enumerate.</param>
        /// <returns>A sequence of values.</returns>
        public static IAsyncEnumerable<T> WithMaterializedErrors<T>(this IAsyncEnumerable<ObjectValue<T>> values)
        {
            return new MaterializedErrorsEnumerable<T>(values);
        }

        sealed class MaterializedErrorsEnumerable<T> : IAsyncEnumerable<T>
        {
            readonly IAsyncEnumerable<ObjectValue<T>> values;

            public MaterializedErrorsEnumerable(IAsyncEnumerable<ObjectValue<T>> values)
            {
                Debug.Assert(values != null);
                this.values = values;
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        sealed class MaterializedErrorsEnumerator<T> : IAsyncEnumerator<T>
        {
            readonly List<Exception> exceptions = new List<Exception>();

            IAsyncEnumerable<ObjectValue<T>> values;
            IAsyncEnumerator<ObjectValue<T>> e;

            public T Current
            {
                get { throw new NotImplementedException(); }
            }

            public MaterializedErrorsEnumerator(IAsyncEnumerable<ObjectValue<T>> values)
            {
                Debug.Assert(values != null);
                this.values = values;
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (e == null)
                {
                    if (values == null)
                    {
                        throw new ObjectDisposedException("MaterializedErrorsEnumerator");
                    }

                    e = values.GetEnumerator();
                    values = null;
                }

                while (await e.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    ObjectValue<T> v = e.Current;

                    if (v == null)
                    {
                        throw new ArgumentNullException("values", "Sequence must not contain null ObjectValues.");
                    }

                    if (v.Exception == null)
                    {
                        if (exceptions.Count == 0)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        exceptions.Add(v.Exception);
                    }
                }

                e.Dispose();
                e = null;

                if (exceptions.Count != 0)
                {
                    throw new AggregateException(exceptions).Flatten();
                }

                return false;
            }

            public void Dispose()
            {
                if (e != null)
                {
                    e.Dispose();
                }

                values = null;
                e = null;
            }
        }
    }
}
