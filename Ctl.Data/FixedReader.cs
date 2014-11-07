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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Reads data rows from a fixed-width file.
    /// </summary>
    public sealed class FixedReader : IDataReader
    {
        const char newlineMask = (char)('\r' ^ '\n'); // used to invert one newline into the other.

        readonly TextReader reader;
        readonly FixedPosition[] positions;
        readonly char[] buffer;
        readonly long[] bufferLines;
        readonly long[] bufferColumns;
        readonly bool readNewLines;
        int len;
        long lineNumber = 1, columnNumber = 0, rowNumber;

        /// <summary>
        /// Initializes a new FixedReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="positions">Record item positions.</param>
        /// <param name="recordWidth">The total width of a record. If not specified, column widths specified in <paramref name="positions"/> will be used.</param>
        /// <param name="readNewLines">If true, newlines between records will be skipped.</param>
        public FixedReader(TextReader reader, IEnumerable<FixedPosition> positions, int? recordWidth = null, bool readNewLines = true)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (positions == null) throw new ArgumentNullException("positions");
            if (!positions.Any()) throw new ArgumentException("positions must contain at least one item.", "widths");

            int buflen = 0;

            foreach(var p in positions)
            {
                if(p.Offset < 0)
                {
                    throw new ArgumentException("Position offset must not be negative.", "positions");
                }

                if (p.Length < 0)
                {
                    throw new ArgumentException("Position length must not be negative.", "positions");
                }

                buflen = Math.Max(buflen, checked(p.Offset - 1 + p.Length));
            }

            if (recordWidth != null)
            {
                buflen = recordWidth.Value;
            }

            buflen = checked(buflen + (readNewLines ? 2 : 0));

            this.reader = reader;
            this.positions = positions.ToArray();
            this.buffer = new char[buflen];
            this.bufferLines = new long[buflen];
            this.bufferColumns = new long[buflen];
            this.readNewLines = readNewLines;
        }

        /// <summary>
        /// The record read.
        /// </summary>
        public RowValue CurrentRow { get; private set; }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <returns>If a record was read, true. Otehrwise, false if the end of the TextReader has been reached.</returns>
        public bool Read()
        {
            len += reader.ReadBlock(buffer, len, buffer.Length - len);
            return ReadRowImpl();
        }

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        public bool TryRead()
        {
            return false;
        }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>If a record was read, true. Otehrwise, false if the end of the TextReader has been reached.</returns>
        public async Task<bool> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            len += await reader.ReadBlockAsync(buffer, len, buffer.Length - len).ConfigureAwait(false);
            return ReadRowImpl();
        }

        bool ReadRowImpl()
        {
            if (len == 0)
            {
                return false;
            }

            // first, calculate character positions.

            long bufLineNo = lineNumber, bufColNo = columnNumber;

            for (int i = 0; i < len; ++i)
            {
                char ch = buffer[i];

                if (ch != '\r' && ch != '\n')
                {
                    bufferLines[i] = bufLineNo;
                    bufferColumns[i] = ++bufColNo;
                }
                else
                {
                    bufferLines[i] = ++bufLineNo;
                    bufferColumns[i] = bufColNo = 0;

                    if (i + 1 < len && buffer[i + 1] == (ch ^ newlineMask))
                    {
                        bufferLines[++i] = bufLineNo;
                        bufferColumns[i] = bufColNo;
                    }
                }
            }

            // now read the row.

            RowValue row = new RowValue(positions.Length, ++rowNumber);

            for (int i = 0; i < positions.Length; ++i)
            {
                int off = positions[i].Offset - 1;

                int readlen = Math.Min(positions[i].Length, len - off);

                while (readlen != 0 && buffer[off + readlen - 1] == ' ')
                {
                    // trim tailing whitespace.
                    --readlen;
                }

                row.Add(new ColumnValue(new string(buffer, off, readlen), bufferLines[off], bufferColumns[off]));
            }

            // clean up a newline at the end.

            if (readNewLines && len > (buffer.Length - 2))
            {
                int off = buffer.Length - 2;
                char ch = buffer[off];

                if (ch == '\r' || ch == '\n')
                {
                    lineNumber = bufferLines[off];
                    columnNumber = bufferColumns[off];

                    ++off;

                    if (off < buffer.Length && buffer[off] == (ch ^ newlineMask))
                    {
                        len = 0;
                    }
                    else
                    {
                        len = buffer.Length - off;
                    }
                }
                else
                {
                    lineNumber = bufferLines[off - 1];
                    columnNumber = bufferColumns[off - 1];
                    len = buffer.Length - off;
                }

                if (len != 0)
                {
                    Array.Copy(buffer, off, buffer, 0, len);
                }
            }
            else
            {
                lineNumber = bufferLines[len - 1];
                columnNumber = bufferColumns[len - 1];
                len = 0;
            }

            CurrentRow = row;
            return true;
        }
    }

    /// <summary>
    /// Reads objects from a fixed-width file.
    /// </summary>
    public sealed class FixedReader<T> : ObjectReader<T>, IDataReader<T>
    {
        readonly FixedReader reader;

        /// <summary>
        /// Initializes a new FixedReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="readNewLines">If true, newlines between records will be skipped.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        public FixedReader(TextReader reader, IFormatProvider formatProvider = null, bool readNewLines = true, bool validate = false)
            : base(formatProvider, validate)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            int width = SerializedType<T>.FixedWidth;

            this.reader = new FixedReader(reader, SerializedType<T>.Positions, width != -1 ? width : (int?)null, readNewLines);

            headers = new CsvHeaderIndex[SerializedType<T>.Positions.Length];
            for (int i = 0; i < headers.Length; ++i)
            {
                headers[i].SerializedIndex = i;
                headers[i].MemberIndex = i;
            }
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

            Deserialize(reader.CurrentRow);
            return true;
        }
    }
}
