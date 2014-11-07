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
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// The value of a single column in a record.
    /// </summary>
    public class ColumnValue
    {
        internal string value;
        internal long lineNumber, columnNumber;

        /// <summary>
        /// The value of this column.
        /// </summary>
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        /// <summary>
        /// The 1-based line index this value started on.
        /// </summary>
        public long LineNumber
        {
            get { return lineNumber; }
            set { lineNumber = value; }
        }

        /// <summary>
        /// The 1-based column index this value started on.
        /// Note this counts UTF-16 code units, not grapheme clusters or even code points.
        /// </summary>
        public long ColumnNumber
        {
            get { return columnNumber; }
            set { columnNumber = value; }
        }

        /// <summary>
        /// Instantiates a new ColumnValue instance.
        /// </summary>
        public ColumnValue()
        {
        }

        /// <summary>
        /// Instantiates a new ColumnValue instance.
        /// </summary>
        /// <param name="value">The value of this column.</param>
        /// <param name="lineNumber">The 1-based line index this value started on.</param>
        /// <param name="columnNumber">
        /// The 1-based column index this value started on.
        /// Note this counts UTF-16 code units, not grapheme clusters or even code points.
        /// </param>
        public ColumnValue(string value, long lineNumber, long columnNumber)
        {
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        /// Gets the Value of this ColumnValue.
        /// </summary>
        /// <returns>The Value of this ColumnValue.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}
