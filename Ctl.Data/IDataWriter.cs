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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// A streaming writer.
    /// </summary>
    public interface IStreamingWriter
    {
        /// <summary>
        /// True if the writer has written any rows yet.
        /// </summary>
        bool HasWrittenRows { get; }

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        void Close();

        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        Task CloseAsync(CancellationToken token);

        /// <summary>
        /// Flushes any buffered data to the underlying storage, and flushes the storage.
        /// </summary>
        void Flush();

        /// <summary>
        /// Flushes any buffered data to the underlying storage, and flushes the storage.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        Task FlushAsync(CancellationToken token);
    }

    /// <summary>
    /// A data row writer.
    /// </summary>
    public interface IDataWriter : IStreamingWriter
    {

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="row">The row to write.</param>
        void WriteRow(IEnumerable<string> row);

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="row">The row to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        Task WriteRowAsync(IEnumerable<string> row, CancellationToken token);
    }

    /// <summary>
    /// An object writer.
    /// </summary>
    /// <typeparam name="T">The type of object to write.</typeparam>
    public interface IDataWriter<T> : IStreamingWriter
    {
        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="item">The object to write.</param>
        void WriteObject(T item);

        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="item">The object to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        Task WriteObjectAsync(T item, CancellationToken token);
    }

    /// <summary>
    /// Extensions methods for the IDataWriter interface.
    /// </summary>
    public static class DataWriterExtensions
    {
        /// <summary>
        /// Closes the writer, flushing any buffered data to the underlying storage.
        /// </summary>
        public static Task CloseAsync(this IStreamingWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            return writer.CloseAsync(CancellationToken.None);
        }

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="row">The row to write.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static Task WriteRowAsync(this IDataWriter writer, IEnumerable<string> row)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (row == null) throw new ArgumentNullException("row");

            return writer.WriteRowAsync(row, CancellationToken.None);
        }

        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="item">The object to write.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static Task WriteObjectAsync<T>(this IDataWriter<T> writer, T item)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (item == null) throw new ArgumentNullException("item");

            return writer.WriteObjectAsync(item, CancellationToken.None);
        }

        /// <summary>
        /// Flushes any buffered data to the underlying storage.
        /// </summary>
        /// <param name="writer">The writer to flush.</param>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public static Task FlushAsync(this IStreamingWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            return writer.FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// Writes rows.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="rows">The rows to write.</param>
        public static void WriteRows(this IDataWriter writer, IEnumerable<IEnumerable<string>> rows)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (rows == null) throw new ArgumentNullException("rows");

            foreach (IEnumerable<string> row in rows)
            {
                if (row == null) throw new ArgumentException("rows must not contain null items.", "rows");

                writer.WriteRow(row);
            }
        }

        /// <summary>
        /// Writes objects.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="items">The items to write.</param>
        public static void WriteObjects<T>(this IDataWriter<T> writer, IEnumerable<T> items)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (items == null) throw new ArgumentNullException("items");

            foreach (T item in items)
            {
                if (item == null) throw new ArgumentException("items must not contain null items.", "items");

                writer.WriteObject(item);
            }
        }

        /// <summary>
        /// Writes rows.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="rows">The rows to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        public static async Task WriteRowsAsync(this IDataWriter writer, IEnumerable<IEnumerable<string>> rows, CancellationToken token)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (rows == null) throw new ArgumentNullException("rows");

            foreach (IEnumerable<string> row in rows)
            {
                if (row == null) throw new ArgumentException("rows must not contain null items.", "rows");

                await writer.WriteRowAsync(row, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Writes rows.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="rows">The rows to write.</param>
        public static Task WriteRowsAsync(this IDataWriter writer, IEnumerable<IEnumerable<string>> rows)
        {
            return WriteRowsAsync(writer, rows, CancellationToken.None);
        }

        /// <summary>
        /// Writes objects.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="items">The items to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        public static async Task WriteObjectsAsync<T>(this IDataWriter<T> writer, IEnumerable<T> items, CancellationToken token)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (items == null) throw new ArgumentNullException("items");

            foreach (T item in items)
            {
                if (item == null) throw new ArgumentException("items must not contain null items.", "items");

                await writer.WriteObjectAsync(item, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Writes objects.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="items">The items to write.</param>
        public static Task WriteObjectsAsync<T>(this IDataWriter<T> writer, IEnumerable<T> items)
        {
            return WriteObjectsAsync(writer, items, CancellationToken.None);
        }

        /// <summary>
        /// Writes rows.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="rows">The rows to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        public static async Task WriteRowsAsync(this IDataWriter writer, IAsyncEnumerable<IEnumerable<string>> rows, CancellationToken token)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (rows == null) throw new ArgumentNullException("rows");

            using (IAsyncEnumerator<IEnumerable<string>> e = rows.GetEnumerator())
            {
                Task<bool> task = e.MoveNext(token);

                while (await task.ConfigureAwait(false))
                {
                    var cur = e.Current;
                    
                    if (cur == null) throw new ArgumentException("rows must not contain null items.", "rows");

                    task = e.MoveNext(token);

                    await writer.WriteRowAsync(cur, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Writes rows.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="rows">The rows to write.</param>
        public static Task WriteRowsAsync(this IDataWriter writer, IAsyncEnumerable<IEnumerable<string>> rows)
        {
            return WriteRowsAsync(writer, rows, CancellationToken.None);
        }

        /// <summary>
        /// Writes objects.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="items">The items to write.</param>
        /// <param name="token">A token used for cancellation.</param>
        public static async Task WriteObjectsAsync<T>(this IDataWriter<T> writer, IAsyncEnumerable<T> items, CancellationToken token)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (items == null) throw new ArgumentNullException("items");

            using (IAsyncEnumerator<T> e = items.GetEnumerator())
            {
                Task<bool> task = e.MoveNext(token);

                while (await task.ConfigureAwait(false))
                {
                    var cur = e.Current;

                    if (cur == null) throw new ArgumentException("items must not contain null items.", "items");

                    task = e.MoveNext(token);

                    await writer.WriteObjectAsync(cur, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Writes objects.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="items">The items to write.</param>
        public static Task WriteObjectsAsync<T>(this IDataWriter<T> writer, IAsyncEnumerable<T> items)
        {
            return WriteObjectsAsync(writer, items, CancellationToken.None);
        }
    }
}
