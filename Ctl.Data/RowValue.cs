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
    /// Raw row values as read from an input stream.
    /// </summary>
    public class RowValue : List<ColumnValue>
    {
        /// <summary>
        /// The 1-based index of the row in the stream.
        /// </summary>
        public long RowNumber { get; set; }

        /// <summary>
        /// Instantiates a new RowValue.
        /// </summary>
        /// <param name="rowNumber">The 1-based index of the row in the input stream.</param>
        public RowValue(long rowNumber = 0)
        {
            RowNumber = rowNumber;
        }

        /// <summary>
        /// Instantiates a new RowValue.
        /// </summary>
        /// <param name="collection">A collection of column values for this row.</param>
        /// <param name="rowNumber">The 1-based index of the row in the input stream.</param>
        public RowValue(IEnumerable<ColumnValue> collection, long rowNumber = 0)
            : base(collection)
        {
            RowNumber = rowNumber;
        }

        /// <summary>
        /// Instantiates a new RowValue.
        /// </summary>
        /// <param name="capacity">The amount of storage to pre-allocate for this row.</param>
        /// <param name="rowNumber">The 1-based index of the row in the input stream.</param>
        public RowValue(int capacity, long rowNumber = 0)
            : base(capacity)
        {
            RowNumber = rowNumber;
        }
    }
}
