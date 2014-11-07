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

using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ctl.Data;
using Ctl.Data.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Ctl.Data.Excel
{
    /// <summary>
    /// Writes a series of rows to an Excel worksheet.
    /// </summary>
    public class ExcelWriter : IDataWriter
    {
        static readonly Task finishedtask = Task.FromResult(false);

        readonly ExcelWorksheet worksheet;
        int pos;

        /// <summary>
        /// Instantiates a new ExcelReader.
        /// </summary>
        /// <param name="worksheet">The worksheet to read from.</param>
        public ExcelWriter(ExcelWorksheet worksheet)
        {
            if (worksheet == null) throw new ArgumentNullException("worksheet");

            this.worksheet = worksheet;
        }

        public void WriteRow(IEnumerable<string> row)
        {
            int col = 0;

            ++pos;

            foreach (var s in row)
            {
                worksheet.Cells[pos, ++col].Value = s;
            }
        }

        public Task WriteRowAsync(IEnumerable<string> row, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            WriteRow(row);
            return finishedtask;
        }

        public void Close()
        {
        }

        public Task CloseAsync(CancellationToken token)
        {
            return finishedtask;
        }

        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken token)
        {
            return finishedtask;
        }

        public bool HasWrittenRows
        {
            get { return pos != 0; }
        }
    }

    public class ExcelWriter<T> : IDataWriter<T>
    {
        static readonly Task finishedtask = Task.FromResult(false);

        readonly ExcelWorksheet worksheet;
        readonly SerializeFunc<T> writeFunc;
        readonly string[] objData;
        readonly IFormatProvider formatProvider;
        readonly bool writeHeaders;
        readonly List<ValidationResult> validationResults = new List<ValidationResult>();
        int rowIdx = 1;

        /// <summary>
        /// Initializes a new CsvWriter.
        /// </summary>
        /// <param name="formatProvider">A format provider to use for any types which implement IFormattable.</param>
        /// <param name="writeHeaders">If true, headers will be written to the file. Otherwise, false.</param>
        /// <param name="validate">If true, validate objects to conform to their data annotations.</param>
        public ExcelWriter(ExcelWorksheet worksheet, IFormatProvider formatProvider = null, bool writeHeaders = true, bool validate = false)
        {
            if (worksheet == null) throw new ArgumentNullException("writer");

            this.worksheet = worksheet;
            this.writeFunc = validate ? SerializedType<T>.ValidatingWriteFunc : SerializedType<T>.WriteFunc;
            this.objData = new string[SerializedType<T>.Headers.Length];
            this.formatProvider = formatProvider;
            this.writeHeaders = writeHeaders;
        }

        public void WriteObject(T item)
        {
            if (item == null) throw new ArgumentNullException("item");

            if (!HasWrittenRows && writeHeaders)
            {
                string[] headers = SerializedType<T>.Headers;
                for(int i = 0; i < headers.Length; ++i)
                {
                    worksheet.Cells[rowIdx, i + 1].Value = headers[i];
                }

                worksheet.Cells[rowIdx, 1, rowIdx, headers.Length].Style.Font.Bold = true;
                worksheet.Cells[rowIdx, 1, rowIdx, headers.Length].AutoFilter = true;
                ++rowIdx;
            }

            writeFunc(objData, item, formatProvider, validationResults);

            for (int i = 0; i < objData.Length; ++i)
            {
                worksheet.Cells[rowIdx, i + 1].Value = objData[i];
            }

            ++rowIdx;
        }

        public Task WriteObjectAsync(T item, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            WriteObject(item);
            return finishedtask;
        }

        public void Close()
        {
        }

        public Task CloseAsync(CancellationToken token)
        {
            return finishedtask;
        }

        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken token)
        {
            return finishedtask;
        }

        public bool HasWrittenRows
        {
            get { return rowIdx > 1; }
        }
    }
}
