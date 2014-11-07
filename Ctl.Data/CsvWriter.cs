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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Ctl.Data.Infrastructure;

namespace Ctl.Data
{
    /// <summary>
    /// Writes raw CSV records.
    /// </summary>
    public sealed class CsvWriter : BufferedWriter, IDataWriter
    {
        readonly char[] quotchars;

        /// <summary>
        /// True if the writer has written any rows yet.
        /// </summary>
        public bool HasWrittenRows { get; private set; }

        /// <summary>
        /// Initializes a new CsvWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to output to.</param>
        /// <param name="separator">The separator to use between record values.</param>
        /// <remarks>The TextWriter is not disposed of by CsvWriter.</remarks>
        public CsvWriter(TextWriter writer, char separator = ',')
            : base(writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            this.quotchars = new[] { separator, '"', '\r', '\n' };
        }

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="row">The row to write.</param>
        public void WriteRow(IEnumerable<string> row)
        {
            WriteLine(row);
            WeakFlush();
        }

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="row">The row to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public Task WriteRowAsync(IEnumerable<string> row, CancellationToken token)
        {
            WriteLine(row);
            return WeakFlushAsync();
        }

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        public void Close()
        {
            StrongFlush();
        }

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        public Task CloseAsync(CancellationToken token)
        {
            return StrongFlushAsync(token);
        }

        /// <summary>
        /// Flushes any buffered data to the TextWriter, and flushes the TextWriter.
        /// </summary>
        public void Flush()
        {
            FullFlush();
        }

        /// <summary>
        /// Flushes any buffered data to the TextWriter, and flushes the TextWriter.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public Task FlushAsync(CancellationToken token)
        {
            return FullFlushAsync(token);
        }

        void WriteLine(IEnumerable<string> items)
        {
            if (items == null) throw new ArgumentNullException("items");

            StringBuilder sb = base.StringBuilder;

            bool first = true;

            foreach (string col in items)
            {
                if (!first) sb.Append(quotchars[0]);
                first = false;

                if (string.IsNullOrEmpty(col))
                {
                    continue;
                }

                if (col.IndexOfAny(quotchars) == -1)
                {
                    sb.Append(col);
                }
                else
                {
                    sb.Append('"').Append(col.IndexOf('"') == -1 ? col : col.Replace("\"", "\"\"")).Append('"');
                }
            }

            sb.Append(Environment.NewLine);

            HasWrittenRows = true;
        }
    }

    /// <summary>
    /// Writes serialized objects to CSV.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <remarks>
    /// Any public fields or properties not annotated with NotMapped will be written. Properties must have a public getter and setter.
    /// The ColumnAttribute annotation is also recognized to map members to differently named columns.
    /// The DataFormatAttribute annotation can be used to specify write formats for objects implementing IFormattable.
    /// </remarks>
    public sealed class CsvWriter<T> : IDataWriter<T>
    {
        readonly CsvWriter writer;
        readonly SerializeFunc<T> writeFunc;
        readonly string[] objData;
        readonly IFormatProvider formatProvider;
        readonly bool writeHeaders;
        readonly List<ValidationResult> validationResults = new List<ValidationResult>();
        SerializeFunc<T> nonValidatingWriteFunc;

        /// <summary>
        /// Initializes a new CsvWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to output to.</param>
        /// <param name="formatProvider">A format provider to use for any types which implement IFormattable.</param>
        /// <param name="separator">The separator to use between record values.</param>
        /// <param name="writeHeaders">If true, headers will be written to the file. Otherwise, false.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        /// <remarks>The TextWriter is not disposed of by CsvWriter.</remarks>
        public CsvWriter(TextWriter writer, IFormatProvider formatProvider = null, char separator = ',', bool writeHeaders = true, bool validate = false)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            this.writer = new CsvWriter(writer, separator);
            this.writeFunc = validate ? SerializedType<T>.ValidatingWriteFunc : SerializedType<T>.WriteFunc;
            this.objData = new string[SerializedType<T>.Headers.Length];
            this.formatProvider = formatProvider;
            this.writeHeaders = writeHeaders;
        }

        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="item">The object to write.</param>
        public void WriteObject(T item)
        {
            if (item == null) throw new ArgumentNullException("item");

            if (!HasWrittenRows && writeHeaders)
            {
                writer.WriteRow(SerializedType<T>.Headers);
            }

            writeFunc(objData, item, formatProvider, validationResults);
            writer.WriteRow(objData);
        }

        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="item">The object to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public Task WriteObjectAsync(T item, CancellationToken token)
        {
            if (item == null) throw new ArgumentNullException("item");

            if (!HasWrittenRows && writeHeaders)
            {
                return WriteObjectAsyncImplFirst(item, token);
            }

            writeFunc(objData, item, formatProvider, validationResults);
            return writer.WriteRowAsync(objData, token);
        }

        async Task WriteObjectAsyncImplFirst(T item, CancellationToken token)
        {
            await writer.WriteRowAsync(SerializedType<T>.Headers, token).ConfigureAwait(false);

            writeFunc(objData, item, formatProvider, validationResults);
            await writer.WriteRowAsync(objData, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes objects to the stream, excluding columns which are null for all objects.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <remarks>
        /// This requires two passes through <paramref name="items"/>,
        /// so keep it lightweight if possible.
        /// </remarks>
        public void WriteObjectsCompact(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (HasWrittenRows) throw new InvalidOperationException("WriteObjectsCompact may only be called if no rows have been written.");

            // Read once to determine empty columns.

            HashSet<int> empty = new HashSet<int>(Enumerable.Range(0, objData.Length));

            using (IEnumerator<T> e = items.GetEnumerator())
            {
                while (empty.Count != 0 && e.MoveNext())
                {
                    if (e.Current == null) throw new ArgumentException("items must not contain any null values.", "items");

                    writeFunc(objData, e.Current, formatProvider, validationResults);

                    while (empty.Count != 0)
                    {
                        int? rem = null;
                        foreach (int i in empty)
                        {
                            if (!string.IsNullOrEmpty(objData[i]))
                            {
                                rem = i;
                                break;
                            }
                        }

                        if (rem == null)
                        {
                            break;
                        }

                        empty.Remove(rem.Value);
                    }
                }
            }

            if (empty.Count == 0)
            {
                // no data to exclude. just use the normal procedure.
                this.WriteObjects(items);
                return;
            }

            if (nonValidatingWriteFunc == null)
            {
                nonValidatingWriteFunc = SerializedType<T>.WriteFunc;
            }

            // determine which indexes to keep, and write only those.

            int[] keep = Enumerable.Range(0, objData.Length).Except(empty).ToArray();
            string[] newdata = new string[keep.Length];

            using (IEnumerator<T> e = items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (!HasWrittenRows)
                    {
                        string[] headers = SerializedType<T>.Headers;

                        for (int i = 0; i < keep.Length; ++i)
                        {
                            newdata[i] = headers[keep[i]];
                        }

                        writer.WriteRow(newdata);
                    }

                    nonValidatingWriteFunc(objData, e.Current, formatProvider, validationResults);

                    for (int i = 0; i < keep.Length; ++i)
                    {
                        newdata[i] = objData[keep[i]];
                    }

                    writer.WriteRow(newdata);
                }
            }
        }

        /// <summary>
        /// Writes objects to the stream, excluding columns which are null for all objects.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This requires two passes through <paramref name="items"/>,
        /// so keep it lightweight if possible.
        /// </remarks>
        public Task WriteObjectsCompactAsync(IAsyncEnumerable<T> items)
        {
            return WriteObjectsCompactAsync(items, CancellationToken.None);
        }

        /// <summary>
        /// Writes objects to the stream, excluding columns which are null for all objects.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This requires two passes through <paramref name="items"/>,
        /// so keep it lightweight if possible.
        /// </remarks>
        public async Task WriteObjectsCompactAsync(IAsyncEnumerable<T> items, CancellationToken token)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (HasWrittenRows) throw new InvalidOperationException("WriteObjectsCompact may only be called if no rows have been written.");

            // Read once to determine empty columns.

            HashSet<int> empty = new HashSet<int>(Enumerable.Range(0, objData.Length));

            using (IAsyncEnumerator<T> e = items.GetEnumerator())
            {
                while(empty.Count != 0 && await e.MoveNext(token).ConfigureAwait(false))
                {
                    if (e.Current == null) throw new ArgumentException("items must not contain any null values.", "items");

                    writeFunc(objData, e.Current, formatProvider, validationResults);

                    while(empty.Count != 0)
                    {
                        int? rem = null;
                        foreach (int i in empty)
                        {
                            if (!string.IsNullOrEmpty(objData[i]))
                            {
                                rem = i;
                                break;
                            }
                        }

                        if (rem == null)
                        {
                            break;
                        }

                        empty.Remove(rem.Value);
                    }
                }
            }

            if (empty.Count == 0)
            {
                // no data to exclude. just use the normal procedure.
                await this.WriteObjectsAsync(items, token).ConfigureAwait(false);
                return;
            }

            if (nonValidatingWriteFunc == null)
            {
                nonValidatingWriteFunc = SerializedType<T>.WriteFunc;
            }
            
            // determine which indexes to keep, and write only those.

            int[] keep = Enumerable.Range(0, objData.Length).Except(empty).ToArray();
            string[] newdata = new string[keep.Length];

            using (IAsyncEnumerator<T> e = items.GetEnumerator())
            {
                while (await e.MoveNext(token).ConfigureAwait(false))
                {
                    if (!HasWrittenRows)
                    {
                        string[] headers = SerializedType<T>.Headers;

                        for(int i = 0; i < keep.Length; ++i)
                        {
                            newdata[i] = headers[keep[i]];
                        }

                        await writer.WriteRowAsync(newdata, token).ConfigureAwait(false);
                    }

                    nonValidatingWriteFunc(objData, e.Current, formatProvider, validationResults);

                    for (int i = 0; i < keep.Length; ++i)
                    {
                        newdata[i] = objData[keep[i]];
                    }

                    await writer.WriteRowAsync(newdata, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// True if the writer has written any rows yet.
        /// </summary>
        public bool HasWrittenRows
        {
            get { return writer.HasWrittenRows; }
        }

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        public void Close()
        {
            writer.Close();
        }

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        public Task CloseAsync(CancellationToken token = default(CancellationToken))
        {
            return writer.CloseAsync(token);
        }

        /// <summary>
        /// Flushes any buffered data to the underlying storage, and flushes the storage.
        /// </summary>
        public void Flush()
        {
            writer.Flush();
        }

        /// <summary>
        /// Flushes any buffered data to the underlying storage, and flushes the storage.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public Task FlushAsync(CancellationToken token = default(CancellationToken))
        {
            return writer.FlushAsync(token);
        }
    }
}
