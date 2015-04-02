/*
    Copyright (c) 2015, CTL Global, Inc.
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

namespace Ctl.Data.Excel
{
    /// <summary>
    /// A set of options for reading objects from Excel files.
    /// </summary>
    public class ExcelObjectOptions : ExcelOptions
    {
        /// <summary>
        /// A format provider used to deserialize objects.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// If true, read a header. Otherwise, use column indexes.
        /// </summary>
        public bool ReadHeader { get; set; }

        /// <summary>
        /// A comparer used to match header values to property names.
        /// </summary>
        public IEqualityComparer<string> HeaderComparer { get; set; }

        /// <summary>
        /// If true, validate objects to conform to their data annotations.
        /// </summary>
        public bool Validate { get; set; }

        /// <summary>
        /// If true, leading and trailing whitespace will be trimmed from column values.
        /// Values consisting of only whitespace will be returned as null.
        /// </summary>
        public bool TrimWhitespace { get; set; }

        public ExcelObjectOptions()
        {
            FormatProvider = null;
            ReadHeader = true;
        }
    }
}
