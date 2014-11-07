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

using Ctl.Data.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Writes raw fixed-width records.
    /// </summary>
    public sealed class FixedWriter : BufferedWriter, IDataWriter
    {
        readonly FixedPosition[] positions;
        readonly bool throwOnTruncation;
        readonly bool writeNewLines;
        readonly char[] rowBuffer;

        /// <summary>
        /// True if the writer has written any rows yet.
        /// </summary>
        public bool HasWrittenRows { get; private set; }

        /// <summary>
        /// Instantiates a new FixedWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to output to.</param>
        /// <param name="positions">Data item positions.</param>
        /// <param name="recordWidth">The total width of a record. If not specified, column widths specified in <paramref name="positions"/> will be used.</param>
        /// <param name="throwOnTruncation">If true, an exception will be thrown if a value would be truncated.</param>
        /// <param name="writeNewLines">If true, write newlines.</param>
        public FixedWriter(TextWriter writer, IEnumerable<FixedPosition> positions, int? recordWidth = null, bool throwOnTruncation = true, bool writeNewLines = true)
            : base(writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (positions == null) throw new ArgumentNullException("positions");
            if (!positions.Any()) throw new ArgumentException("Positions must contain at least one item.", "positions");

            int buflen = 0;

            foreach (var p in positions)
            {
                if (p.Offset <= 0)
                {
                    throw new ArgumentException("Position offset must be greater than zero.", "positions");
                }

                if (p.Length <= 0)
                {
                    throw new ArgumentException("Position length must be greater than zero.", "positions");
                }

                buflen = Math.Max(buflen, checked(p.Offset - 1 + p.Length));
            }

            if (recordWidth != null)
            {
                buflen = recordWidth.Value;
            }

            buflen = checked(buflen + (writeNewLines ? Environment.NewLine.Length : 0));

            this.positions = positions.ToArray();
            this.throwOnTruncation = throwOnTruncation;
            this.writeNewLines = writeNewLines;

            this.rowBuffer = new char[buflen];

            for (int i = 0; i < buflen; ++i)
            {
                this.rowBuffer[i] = ' ';
            }

            if (writeNewLines)
            {
                Environment.NewLine.CopyTo(0, this.rowBuffer, buflen - Environment.NewLine.Length, Environment.NewLine.Length);
            }
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

        void WriteLine(IEnumerable<string> row)
        {
            if (row == null) throw new ArgumentNullException("row");

            int idx = 0;

            foreach (string item in row)
            {
                if (idx >= positions.Length)
                {
                    if (throwOnTruncation)
                    {
                        throw new ArgumentOutOfRangeException("row", string.Format("FixedWriter has not been configured with enough item positions to write this row. Items starting at index {0} would be truncated during writing.", idx));
                    }

                    break;
                }

                var position = positions[idx];

                if (item != null)
                {
                    int off = position.Offset - 1;

                    if (item.Length <= position.Length)
                    {
                        item.CopyTo(0, rowBuffer, off, item.Length);

                        for (int first = off + item.Length, last = off + position.Length; first != last; ++first)
                        {
                            rowBuffer[first] = ' ';
                        }
                    }
                    else if (throwOnTruncation)
                    {
                        throw new ArgumentOutOfRangeException("row", string.Format("Item at index {0} would be truncated during writing.", idx));
                    }
                    else
                    {
                        // truncate!
                        item.CopyTo(0, rowBuffer, position.Offset - 1, position.Length);
                    }
                }
                else
                {
                    for (int first = position.Offset - 1, last = first + position.Length; first != last; ++first)
                    {
                        rowBuffer[first] = ' ';
                    }
                }

                ++idx;
            }

            base.StringBuilder.Append(rowBuffer);

            HasWrittenRows = true;
        }
    }

    /// <summary>
    /// Writes serialized objects to fixed-width.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <remarks>
    /// Any public fields or properties not annotated with NotMapped will be written. Properties must have a public getter and setter.
    /// The DataWidthAttribute annotation must be used to specify a member's width, otherwise it will be assumed to be 0.
    /// The DataFormatAttribute annotation can be used to specify write formats for objects implementing IFormattable.
    /// </remarks>
    public sealed class FixedWriter<T> : IDataWriter<T>
    {
        readonly FixedWriter writer;
        readonly SerializeFunc<T> writeFunc;
        readonly string[] objData;
        readonly IFormatProvider formatProvider;
        readonly List<ValidationResult> validationResults = new List<ValidationResult>();

        /// <summary>
        /// Initializes a new FixedWriter.
        /// </summary>
        /// <param name="writer">The TextWriter to output to.</param>
        /// <param name="throwOnTruncation">If true, an exception will be thrown if a value would be truncated.</param>
        /// <param name="formatProvider">A format provider to use for any types which implement IFormattable.</param>
        /// <param name="writeNewLines">If true, newlines will be written between records.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        /// <remarks>The TextWriter is not disposed of by FixedWriter.</remarks>
        public FixedWriter(TextWriter writer, bool throwOnTruncation = true, IFormatProvider formatProvider = null, bool writeNewLines = true, bool validate = false)
        {
            if (writer == null) throw new ArgumentNullException("writer");

            int width = SerializedType<T>.FixedWidth;

            this.writer = new FixedWriter(writer, SerializedType<T>.Positions, width != -1 ? width : (int?)null, throwOnTruncation, writeNewLines);
            this.writeFunc = validate ? SerializedType<T>.ValidatingWriteFunc : SerializedType<T>.WriteFunc;
            this.objData = new string[SerializedType<T>.Headers.Length];
            this.formatProvider = formatProvider;
        }

        /// <summary>
        /// Writes an object.
        /// </summary>
        /// <param name="item">The object to write.</param>
        public void WriteObject(T item)
        {
            if (item == null) throw new ArgumentNullException("item");

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

            writeFunc(objData, item, formatProvider, validationResults);
            return writer.WriteRowAsync(objData, token);
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
