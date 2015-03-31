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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Reads raw CSV records from a TextReader.
    /// </summary>
    /// <remarks>
    /// This reads unquoted and quoted CSV. Quoted CSV may contain newlines. Quotes inside of quotes
    /// will be considered part of a record if they are not immediately followed by a separator or newline.
    /// </remarks>
    public sealed class CsvReader : IDataReader
    {
        internal const int DefaultBufferLength = 4096;
        const int NewlineMask = '\r' ^ '\n';

        readonly TextReader reader;
        readonly char[] buffer;
        readonly char separator;
        readonly bool parseMidQuotes;

        int position, length;

        ParseState state = ParseState.Begin;
        bool endOfStream;

        long lineNumber, colNumber, rowNumber;
        long startLineNumber, startColNumber;

        RowValue items;
        int prevItemCount;
        string itemBuffer;

        /// <summary>
        /// Instantiates a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="separator">The column separator.</param>
        /// <param name="parseMidQuotes">If true, unescaped quotes in the middle of a quoted value are assumed to be part of the value. Otherwise, require strict escaping.</param>
        [Obsolete("This constructor is obsolete. The overload taking a CsvOptions object should be used instead.")]
        public CsvReader(TextReader reader, char separator = ',', bool parseMidQuotes = false)
            : this(reader, new CsvOptions { Separator = separator, ParseMidQuotes = parseMidQuotes })
        {
        }

        /// <summary>
        /// Instantiates a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="separator">The column separator.</param>
        /// <param name="parseMidQuotes">If true, unescaped quotes in the middle of a quoted value are assumed to be part of the value. Otherwise, require strict escaping.</param>
        /// <param name="bufferLength">The buffer length to use while reading.</param>
        [Obsolete("This constructor is obsolete. The overload taking a CsvOptions object should be used instead.")]
        public CsvReader(TextReader reader, char separator, bool parseMidQuotes, int bufferLength)
            : this(reader, new CsvOptions { Separator = separator, ParseMidQuotes = parseMidQuotes, BufferLength = bufferLength })
        {
        }

        /// <summary>
        /// Instantiates a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="options">Options to use when reading the file. If not specified, default options will be used.</param>
        public CsvReader(TextReader reader, CsvOptions options)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            if (options == null)
            {
                options = new CsvOptions();
            }

            int bufferLength = options.BufferLength;

#if DEBUG
            // at least 3 characters of lookahead are required.
            if (bufferLength < 3) bufferLength = 3;
#else
            // perf is really going to suck if you use an abysmally small buffer.
            if (bufferLength < 64) bufferLength = 64;
#endif

            this.reader = reader;
            this.buffer = new char[bufferLength];
            this.separator = options.Separator;
            this.parseMidQuotes = options.ParseMidQuotes;
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
            if (items == null)
            {
                items = new RowValue(prevItemCount, ++rowNumber);
            }

            if (position == length && endOfStream)
            {
                return false;
            }

            while (true)
            {
                switch(Parse())
                {
                    case ParseResult.Row:
                        prevItemCount = items.Count;
                        CurrentRow = items;
                        items = null;
                        return true;
                    case ParseResult.Done:
                        return false;
                }

                length -= position;

                if (length > 0)
                {
                    Array.Copy(buffer, position, buffer, 0, length);
                }

                position = 0;

                int readlen = reader.Read(buffer, length, buffer.Length - length);

                length += readlen;
                endOfStream = readlen == 0;
            }
        }

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        public bool TryRead()
        {
            if (items == null)
            {
                items = new RowValue(prevItemCount, ++rowNumber);
            }

            if (position == length || Parse() != ParseResult.Row)
            {
                return false;
            }

            prevItemCount = items.Count;
            CurrentRow = items;
            items = null;

            return true;
        }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>If a record was read, true. Otehrwise, false if the end of the TextReader has been reached.</returns>
        public Task<bool> ReadAsync(CancellationToken token)
        {
            if (items == null)
            {
                items = new RowValue(prevItemCount, ++rowNumber);
            }

            if (position == length && endOfStream)
            {
                return Constants.FalseTask;
            }

            token.ThrowIfCancellationRequested();

            switch (Parse())
            {
                case ParseResult.Row:
                    prevItemCount = items.Count;
                    CurrentRow = items;
                    items = null;
                    return Constants.TrueTask;
                case ParseResult.Done:
                    return Constants.FalseTask;
            }

            return ReadAsyncImpl(token);
        }

        async Task<bool> ReadAsyncImpl(CancellationToken token)
        {
            while (true)
            {
                length -= position;

                if (length > 0)
                {
                    Array.Copy(buffer, position, buffer, 0, length);
                }

                position = 0;

                int readlen = await reader.ReadAsync(buffer, length, buffer.Length - length).ConfigureAwait(false);

                length += readlen;
                endOfStream = readlen == 0;

                token.ThrowIfCancellationRequested();

                switch (Parse())
                {
                    case ParseResult.Row:
                        prevItemCount = items.Count;
                        CurrentRow = items;
                        items = null;
                        return true;
                    case ParseResult.Done:
                        return false;
                }
            }
        }

        /// <remarks>
        /// keeping it all in one method is for performance. goto is for performance. thanks for understanding :)
        /// </remarks>
        ParseResult Parse()
        {
            int idx;
            char ch;

            switch (state)
            {
                case ParseState.Begin:
                    if (position == length)
                    {
                        if (!endOfStream)
                        {
                            return ParseResult.NeedMore;
                        }

                        if (colNumber == 0)
                        {
                            return ParseResult.Done;
                        }

                        items.Add(new ColumnValue(null, lineNumber + 1, colNumber + 1));

                        return ParseResult.Row;
                    }

                    startLineNumber = lineNumber;
                    startColNumber = colNumber;

                    ch = buffer[position];

                    if (ch == '"')
                    {
                        ++position; ++colNumber;
                        state = ParseState.QuotedEnd;
                        goto case ParseState.QuotedEnd;
                    }

                    state = ParseState.End;
                    goto case ParseState.End;
                case ParseState.End:
                    idx = position;

                    while (idx < length)
                    {
                        ch = buffer[idx];

                        if (ch == separator)
                        {
                            colNumber += idx - position + 1;
                            Take(idx - position, 1);

                            items.Add(new ColumnValue(itemBuffer, startLineNumber + 1, startColNumber + 1));
                            itemBuffer = null;

                            state = ParseState.Begin;
                            goto case ParseState.Begin;
                        }

                        if (ch == '\r' || ch == '\n')
                        {
                            colNumber += idx - position;
                            Take(idx - position, 0);

                            if (idx + 1 == length)
                            {
                                if (!endOfStream)
                                {
                                    return ParseResult.NeedMore;
                                }

                                Take(0, 1);
                                goto haveRow;
                            }

                            Take(0, buffer[idx + 1] == (ch ^ NewlineMask) ? 2 : 1);
                            colNumber = 0;
                            ++lineNumber;

                            goto haveRow;
                        }

                        ++idx;
                    }

                    colNumber += idx - position;
                    Take(idx - position, 0);

                needMoreOrEnd:
                    if (!endOfStream)
                    {
                        return ParseResult.NeedMore;
                    }

                haveRow:
                    items.Add(new ColumnValue(itemBuffer, startLineNumber + 1, startColNumber + 1));
                    itemBuffer = null;

                    state = ParseState.Begin;
                    return ParseResult.Row;
                case ParseState.QuotedEnd:
                    idx = position;

                    while (idx < length)
                    {
                        ch = buffer[idx];

                        if (ch == '\r' || ch == '\n')
                        {
                            if (idx + 1 == length)
                            {
                                Take(idx - position, 0);
                                break;
                            }

                            idx += buffer[idx + 1] == (ch ^ NewlineMask) ? 2 : 1;
                            colNumber = 0;
                            ++lineNumber;

                            continue;
                        }

                        if (ch != '"')
                        {
                            ++idx;
                            ++colNumber;
                            continue;
                        }

                        if (idx + 1 == length)
                        {
                            colNumber += endOfStream ? 1 : 0;
                            Take(idx - position, endOfStream ? 1 : 0);
                            goto needMoreOrEnd;
                        }

                        ch = buffer[idx + 1];

                        if (ch == '"')
                        {
                            Take(idx - position + 1, 1);
                            idx += 2;
                            colNumber += 2;
                            continue;
                        }

                        if (ch == separator)
                        {
                            colNumber += 2;
                            Take(idx - position, 2);

                            items.Add(new ColumnValue(itemBuffer, startLineNumber + 1, startColNumber + 1));
                            itemBuffer = null;

                            state = ParseState.Begin;
                            goto case ParseState.Begin;
                        }

                        if (ch == '\r' || ch == '\n')
                        {
                            Take(idx - position, 0);

                            if (idx + 2 == length)
                            {
                                if (!endOfStream)
                                {
                                    return ParseResult.NeedMore;
                                }

                                Take(0, 2);
                                goto haveRow;
                            }

                            Take(0, buffer[idx + 2] == (ch ^ NewlineMask) ? 3 : 2);
                            colNumber = 0;
                            ++lineNumber;

                            goto haveRow;
                        }

                        if (!parseMidQuotes)
                        {
                            // free-floating quote. no way to know if it's a typo or intentional, so err on the side of caution.
                            throw new ParseException("Quoted value contains an unescaped quote not followed by a separator or newline.", lineNumber + 1, colNumber + 1);
                        }

                        ++idx;
                        ++colNumber;
                    }

                    Take(idx - position, 0);

                    if (endOfStream)
                    {
                        throw new ParseException("Unexpected end of stream in middle of quoted value.", lineNumber + 1, colNumber + 1);
                    }

                    return ParseResult.NeedMore;
            }

            throw new ParseException("Parser reached a point it should never reach. Please report this bug!");
        }

        void Take(int takeCount, int skipCount)
        {
            Debug.Assert(takeCount >= 0);
            Debug.Assert(skipCount >= 0);
            Debug.Assert(takeCount + skipCount <= length);

            if (takeCount > 0)
            {
                string s = new string(buffer, position, takeCount);
                itemBuffer = itemBuffer == null ? s : itemBuffer + s;
            }

            position += takeCount + skipCount;
        }

        enum ParseState
        {
            Begin,
            End,
            QuotedEnd
        }

        enum ParseResult
        {
            Row,
            NeedMore,
            Done
        }
    }

    /// <summary>
    /// Reads serialized objects from a TextReader
    /// </summary>
    /// <typeparam name="T">The type of object to read.</typeparam>
    /// <remarks>
    /// Any public fields or properties not annotated with NotMapped will be read. Properties must have a public getter and setter.
    /// The Column annotation is recognized to map members to differently named columns, and to customize their order.
    /// </remarks>
    public sealed class CsvReader<T> : HeaderedObjectReader<T>
    {
        /// <summary>
        /// Initializes a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="separator">The column separator.</param>
        /// <param name="parseMidQuotes">If true, unescaped quotes in the middle of a quoted value are assumed to be part of the value. Otherwise, require strict escaping.</param>
        /// <param name="readHeader">If true, read a header. Otherwise, use column indexes.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        [Obsolete("This constructor is obsolete. The overload taking a CsvObjectOptions object should be used instead.")]
        public CsvReader(TextReader reader, IFormatProvider formatProvider = null, char separator = ',', bool parseMidQuotes = false, bool readHeader = true, bool validate = false)
            : this(reader, new CsvObjectOptions { FormatProvider = formatProvider, Separator = separator, ParseMidQuotes = parseMidQuotes, ReadHeader = readHeader, Validate = validate })
        {
        }

        /// <summary>
        /// Initializes a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="separator">The column separator.</param>
        /// <param name="parseMidQuotes">If true, unescaped quotes in the middle of a quoted value are assumed to be part of the value. Otherwise, require strict escaping.</param>
        /// <param name="readHeader">If true, read a header. Otherwise, use column indexes.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        /// <param name="bufferLength">The internal buffer length to use. Higher values will trade memory for performance.</param>
        [Obsolete("This constructor is obsolete. The overload taking a CsvObjectOptions object should be used instead.")]
        public CsvReader(TextReader reader, IFormatProvider formatProvider, char separator, bool parseMidQuotes, bool readHeader, bool validate, int bufferLength)
            : this(reader, new CsvObjectOptions { FormatProvider = formatProvider, Separator = separator, ParseMidQuotes = parseMidQuotes, ReadHeader = readHeader, Validate = validate, BufferLength = bufferLength })
        {
        }

        /// <summary>
        /// Initializes a new CsvReader.
        /// </summary>
        /// <param name="reader">The TextReader to read from.</param>
        /// <param name="options">A set of options to use. If not specified, defaults will be used.</param>
        public CsvReader(TextReader reader, CsvObjectOptions options)
            : base(new CsvReader(reader, options), (options ?? defOpts).FormatProvider, (options ?? defOpts).Validate, (options ?? defOpts).ReadHeader, (options ?? defOpts).HeaderComparer)
        {
        }

        static readonly CsvObjectOptions defOpts = new CsvObjectOptions();
    }
}
