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
    /// An exception resulting from parsing a file.
    /// </summary>
    [Serializable]
    public class ParseException : Exception
    {
        /// <summary>
        /// The 1-based index of the line in the file. Note this does not correspond to row, as a row can contain multiple lines.
        /// </summary>
        public long LineNumber { get; private set; }

        /// <summary>
        /// The 1-based index of the column in the line. Note this only takes into account code units, not visible grapheme clusters.
        /// </summary>
        public long ColumnNumber { get; private set; }

        /// <summary>
        /// Instantiates a new ParseException.
        /// </summary>
        public ParseException()
        {
        }

        /// <summary>
        /// Instantiates a new ParseException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public ParseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiates a new ParseException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="lineNumber">The line number of the input which caused the exception.</param>
        /// <param name="columnNumber">The column number of the input which caused the exception.</param>
        public ParseException(string message, long lineNumber, long columnNumber)
            : base(message)
        {
            ColumnNumber = columnNumber;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Instantiates a new ParseException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        /// <param name="lineNumber">The line number of the input which caused the exception.</param>
        /// <param name="columnNumber">The column number of the input which caused the exception.</param>
        /// <param name="innerException">An inner exception.</param>
        public ParseException(string message, long lineNumber, long columnNumber, Exception innerException)
            : base(message, innerException)
        {
            ColumnNumber = columnNumber;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Instantiates a new ParseException from a serialized instance.
        /// </summary>
        /// <param name="info">Serialization info to deserialize from.</param>
        /// <param name="context">A context to use while deserializing.</param>
        protected ParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            ColumnNumber = info.GetInt64("ColumnNumber");
            LineNumber = info.GetInt64("LineNumber");
        }

        /// <summary>
        /// Serializes the ParseException for remoting.
        /// </summary>
        /// <param name="info">Serialization info to store data to.</param>
        /// <param name="context">A context to use while serializing.</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
            info.AddValue("ColumnNumber", ColumnNumber);
            info.AddValue("LineNumber", LineNumber);
        }
    }
}
