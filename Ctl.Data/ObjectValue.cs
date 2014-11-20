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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// A deserialized object value.
    /// </summary>
    /// <typeparam name="T">The type of object deserialized.</typeparam>
    public sealed class ObjectValue<T> : ObjectValue
    {
        /// <summary>
        /// The deserialized object.
        /// If deserialization resulted in an error, an exception will be thrown.
        /// </summary>
        public T Value
        {
            get
            {
                if (Exception != null)
                {
                    throw new AggregateException("Value is not set due to serialization errors. See InnerExceptions for details.", Exception).Flatten();
                }

                return UnvalidatedValue;
            }
            private set
            {
                UnvalidatedValue = value;
            }
        }

        /// <summary>
        /// A deserialized but unvalidated value.
        /// Unlike Value, will not throw an exception if there were validation errors.
        /// </summary>
        public T UnvalidatedValue { get; private set; }

        /// <summary>
        /// Instantiates a new ObjectValue.
        /// </summary>
        public ObjectValue()
        {
        }

        /// <summary>
        /// Instantiates a new ObjectValue.
        /// </summary>
        /// <param name="rawValues">The raw row values which the deserialized object was read from.</param>
        /// <param name="value">The deserialized object.</param>
        public ObjectValue(RowValue rawValues, T value)
        {
            RawValues = rawValues;
            Value = value;
            HasUnvalidatedValue = true;
        }

        /// <summary>
        /// Instantiates a new ObjectValue for a deserialization error.
        /// </summary>
        /// <param name="rawValues">The raw row values which the deserialized object was read from.</param>
        /// <param name="exceptions">The errors which prevented an object from deserializing.</param>
        public ObjectValue(RowValue rawValues, IEnumerable<Exception> exceptions)
            : base(rawValues, exceptions)
        {
        }

        internal ObjectValue(RowValue rawValues, IEnumerable<ValidationResult> validationErrors, T value)
        {
            long lineNumber = long.MaxValue, colNumber = 0;

            foreach(var c in rawValues)
            {
                if(c.LineNumber < lineNumber)
                {
                    lineNumber = c.LineNumber;
                    colNumber = c.ColumnNumber;
                }
            }

            if(lineNumber == long.MaxValue)
            {
                lineNumber = 0;
            }

            RawValues = rawValues;
            Exception = new AggregateException(new ValidationException(validationErrors, value, lineNumber, colNumber)).Flatten();
            UnvalidatedValue = value;
            HasUnvalidatedValue = true;
        }
    }

    /// <summary>
    /// A deserialized object value.
    /// </summary>
    public class ObjectValue
    {
        /// <summary>
        /// The 1-based index of the row in the stream.
        /// </summary>
        public long RowNumber { get { return RawValues.RowNumber; } }

        /// <summary>
        /// The 1-based line index this value started on.
        /// </summary>
        public long LineNumber { get { return RawValues.Count != 0 ? RawValues[0].LineNumber : 0; } }

        /// <summary>
        /// The 1-based column index this value started on.
        /// Note this counts UTF-16 code units, not grapheme clusters or even code points.
        /// </summary>
        public long ColumnNumber { get { return RawValues.Count != 0 ? RawValues[0].ColumnNumber : 0; } }

        /// <summary>
        /// The raw row values which the deserialized object was read from.
        /// </summary>
        public RowValue RawValues { get; protected set; }

        /// <summary>
        /// The Exception which caused deserialization to fail.
        /// </summary>
        public AggregateException Exception { get; protected set; }

        /// <summary>
        /// If true, the object deserialized successfully but failed validation.
        /// UnvalidatedValue will contain the object.
        /// </summary>
        public bool HasUnvalidatedValue { get; protected set; }

        /// <summary>
        /// Instantiates a new ObjectValue.
        /// </summary>
        protected internal ObjectValue()
        {
        }

        /// <summary>
        /// Instantiates a new ObjectValue for a deserialization error.
        /// </summary>
        /// <param name="rawValues">The raw row values which the deserialized object was read from.</param>
        /// <param name="exceptions">The errors which prevented an object from deserializing.</param>
        protected internal ObjectValue(RowValue rawValues, IEnumerable<Exception> exceptions)
        {
            RawValues = rawValues;
            Exception = new AggregateException(exceptions).Flatten();
        }
    }
}
