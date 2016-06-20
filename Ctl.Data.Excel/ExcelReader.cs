/*
    Copyright (c) 2014, CTL Global, Inc.
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
using System.Threading.Tasks;
using OfficeOpenXml;
using System.Globalization;

namespace Ctl.Data.Excel
{
    /// <summary>
    /// Reads an Excel worksheet as a series of rows.
    /// </summary>
    public class ExcelReader : IDataReader
    {
        readonly ExcelRange range;
        readonly bool trimWhitespace;
        readonly bool readFormatted;
        readonly IFormatProvider unformattedFormat;

        int pos, endRow;
        int prevRowSize = 0;

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        public ExcelReader(ExcelWorksheet worksheet)
            : this(worksheet, null)
        {
        }

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        /// <param name="options">Options for reading the worksheet. If not specified, defaults will be used.</param>
        public ExcelReader(ExcelWorksheet worksheet, ExcelOptions options)
            : this(GetRange(worksheet), options)
        {
            if (worksheet == null) throw new ArgumentNullException(nameof(worksheet));
        }

        static ExcelRange GetRange(ExcelWorksheet worksheet)
        {
            ExcelAddressBase dim = worksheet.Dimension;
            if (dim == null) return null;

            return worksheet.Cells[dim.Start.Row, dim.Start.Column, dim.End.Row, dim.End.Column];
        }

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="range">The range to read from.</param>
        public ExcelReader(ExcelRange range)
            : this(range, null)
        {
        }

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="range">The range to read from.</param>
        /// <param name="options">Options for reading the worksheet. If not specified, defaults will be used.</param>
        public ExcelReader(ExcelRange range, ExcelOptions options)
        {
            if (options != null)
            {
                this.trimWhitespace = options.TrimWhitespace;
                this.readFormatted = options.ReadFormatted;
                this.unformattedFormat = options.UnformattedFormat ?? CultureInfo.CurrentCulture;
            }

            if ((range?.Rows ?? 0) != 0)
            {
                this.range = range;
                this.pos = range.Start.Row - 1;
                this.endRow = range.End.Row;

                if (options?.TrimTrailingRows != false)
                {
                    while(endRow > pos)
                    {
                        ExcelRange rowRange = range[endRow, range.Start.Column, endRow, range.End.Column];
                        bool isWhite = true;

                        foreach (var cell in rowRange)
                        {
                            string v = GetCellValue(cell);
                            if (!string.IsNullOrWhiteSpace(v))
                            {
                                isWhite = false;
                                break;
                            }
                        }

                        if (!isWhite) break;
                        --endRow;
                    }
                }
            }
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
            return TryRead();
        }

        /// <summary>
        /// Reads a record.
        /// </summary>
        /// <param name="token">A token used for cancellation.</param>
        /// <returns>If a record was read, true. Otehrwise, false if the end of the TextReader has been reached.</returns>
        public Task<bool> ReadAsync(System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(TryRead());
        }

        string GetCellValue(ExcelRangeBase cell)
        {
            if (readFormatted && cell.Style?.Numberformat != null)
            {
                return cell.Text;
            }

            object val = cell.Value;
            switch (Convert.GetTypeCode(val))
            {
                case TypeCode.Boolean:
                    return Convert.ToBoolean(val).ToString(unformattedFormat);
                case TypeCode.DateTime:
                    return Convert.ToDateTime(val).ToString("O", unformattedFormat);
                case TypeCode.Single:
                    return Convert.ToSingle(val).ToString("R", unformattedFormat);
                case TypeCode.Double:
                    return Convert.ToDouble(val).ToString("R", unformattedFormat);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(val).ToString(unformattedFormat);
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return Convert.ToInt64(val).ToString(unformattedFormat);
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToUInt64(val).ToString(unformattedFormat);
                default:
                    if (val == null) return null;
                    return Convert.ToString(val);
            }
        }

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        public bool TryRead()
        {
            if (range == null)
            {
                return false;
            }

            if (++pos > endRow)
            {
                return false;
            }

            ExcelRange rowRange = range[pos, range.Start.Column, pos, range.End.Column];
            ExcelRowValue row = new ExcelRowValue(prevRowSize, pos, rowRange.Address);

            foreach (var cell in rowRange)
            {
                string value = GetCellValue(cell);

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (trimWhitespace)
                {
                    value = value.Trim();
                }

                int colNum = new ExcelAddress(cell.Address).Start.Column;

                while (row.Count != (colNum - 1))
                {
                    int blankColNum = row.Count + 1;
                    row.Add(new ExcelColumnValue(null, pos, row.Count, new ExcelAddress(pos, blankColNum, pos, blankColNum).Address));
                }

                row.Add(new ExcelColumnValue(value, pos, colNum, cell.Address));
            }

            CurrentRow = row;
            prevRowSize = row.Count;
            return true;
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
    public sealed class ExcelReader<T> : HeaderedObjectReader<T>
    {
        /// <summary>
        /// Initializes a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        /// <param name="formatProvider">A format provider used to deserialize objects.</param>
        /// <param name="readHeader">If true, read a header. Otherwise, use column indexes.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        [Obsolete("This constructor is obsolete. The overload taking a ExcelObjectOptions object should be used instead.")]
        public ExcelReader(ExcelWorksheet worksheet, IFormatProvider formatProvider = null, bool readHeader = true, bool validate = false)
            : this(worksheet, new ExcelObjectOptions { FormatProvider = formatProvider, ReadHeader = readHeader, Validate = validate })
        {
        }

        /// <summary>
        /// Initializes a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        /// <param name="options">A set of options to use. If not specified, defaults will be used.</param>
        public ExcelReader(ExcelWorksheet worksheet, ExcelObjectOptions options)
            : base(new ExcelReader(worksheet, options), (options ?? defOpts).FormatProvider, (options ?? defOpts).Validate, (options ?? defOpts).ReadHeader, (options ?? defOpts).HeaderComparer)
        {
        }

        /// <summary>
        /// Initializes a new ExcelReader.
        /// </summary>
        /// <param name="range">The range to read from.</param>
        /// <param name="options">A set of options to use. If not specified, defaults will be used.</param>
        public ExcelReader(ExcelRange range, ExcelObjectOptions options)
            : base(new ExcelReader(range, options), (options ?? defOpts).FormatProvider, (options ?? defOpts).Validate, (options ?? defOpts).ReadHeader, (options ?? defOpts).HeaderComparer)
        {
        }

        static readonly ExcelObjectOptions defOpts = new ExcelObjectOptions();
    }
}
