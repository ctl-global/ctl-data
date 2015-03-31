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

namespace Ctl.Data.Excel
{
    /// <summary>
    /// Reads an Excel worksheet as a series of rows.
    /// </summary>
    public class ExcelReader : IDataReader
    {
        readonly ExcelWorksheet worksheet;
        int pos;
        bool posSet = false;
        int prevRowSize = 0;

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        public ExcelReader(ExcelWorksheet worksheet)
        {
            if (worksheet == null) throw new ArgumentNullException("worksheet");

            this.worksheet = worksheet;
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

        /// <summary>
        /// Tries to read a record using buffered data, without performing any I/O.
        /// </summary>
        /// <returns>If a record was read, true. Otherwise, false to indicate an exhausted buffer, indicating ReadAsync() should be called again.</returns>
        public bool TryRead()
        {
            if (!posSet)
            {
                if (worksheet.Dimension == null)
                {
                    return false;
                }

                pos = worksheet.Dimension.Start.Row - 1;
                posSet = true;
            }

            if (++pos > worksheet.Dimension.End.Row)
            {
                return false;
            }

            ExcelRange range = worksheet.Cells[pos, worksheet.Dimension.Start.Column, pos, worksheet.Dimension.End.Column];

            ExcelRowValue row = new ExcelRowValue(prevRowSize, pos, range.Address);

            foreach (var cell in range)
            {
                string value = cell.Value != null ? cell.Value.ToString() : null;

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
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
            : base(new ExcelReader(worksheet), (options ?? defOpts).FormatProvider, (options ?? defOpts).Validate, (options ?? defOpts).ReadHeader, (options ?? defOpts).HeaderComparer)
        {
        }

        static readonly ExcelObjectOptions defOpts = new ExcelObjectOptions();
    }
}
