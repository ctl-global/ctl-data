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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Provides a base class for IDataReaders which perform object deserialization.
    /// </summary>
    /// <typeparam name="T">The type of object to read.</typeparam>
    public class ObjectReader<T>
    {
        readonly DeserializeFunc<T> readFunc;
        readonly IFormatProvider formatProvider;

        internal CsvHeaderIndex[] headers;
        internal List<ValidationResult> missingHeaderErrors;

        List<Exception> exceptions = new List<Exception>();
        List<ValidationResult> validationErrors = new List<ValidationResult>();

        /// <summary>
        /// The object read.
        /// </summary>
        public ObjectValue<T> CurrentObject { get; private set; }

        /// <summary>
        /// Instantiates a new ObjectReader instance.
        /// </summary>
        /// <param name="formatProvider">The format provider to use for deserialization.</param>
        /// <param name="validate">If true, objects are validated.</param>
        protected ObjectReader(IFormatProvider formatProvider, bool validate)
        {
            this.formatProvider = formatProvider;
            this.readFunc = validate ? SerializedType<T>.ValidatingReadFunc : SerializedType<T>.ReadFunc;
        }

        /// <summary>
        /// Deserializes a row into an object.
        /// </summary>
        /// <param name="row">The row to deserialize.</param>
        protected void Deserialize(RowValue row)
        {
            Debug.Assert(row != null);

            if (missingHeaderErrors != null)
            {
                validationErrors.AddRange(missingHeaderErrors);
            }

            CurrentObject = readFunc(headers, row, formatProvider, exceptions, validationErrors);

            if (exceptions.Count != 0) exceptions = new List<Exception>();
            if (validationErrors.Count != 0) validationErrors = new List<ValidationResult>();
        }
    }
}
