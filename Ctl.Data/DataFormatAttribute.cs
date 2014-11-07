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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Specifies a custom format to use when serializing objects supporting IFormattable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DataFormatAttribute : Attribute
    {
        /// <summary>
        /// A format to use when serializing objects supporting IFormattable, and used when deserializing objects with a compatible ParseExact method.
        /// </summary>
        public string Format { get; private set; }

        /// <summary>
        /// NumberStyles to use when deserializing supporting types.
        /// </summary>
        public NumberStyles? NumberStyles { get; private set; }

        /// <summary>
        /// DateTimeStyles to use when deserializing supporting types.
        /// </summary>
        public DateTimeStyles? DateTimeStyles { get; private set; }

        /// <summary>
        /// Initializes a new DataFormatAttribute.
        /// </summary>
        /// <param name="format">A format to use when serializing objects supporting IFormattable, and used when deserializing objects with a compatible ParseExact method.</param>
        public DataFormatAttribute(string format)
        {
            Format = format;
        }

        /// <summary>
        /// Initializes a new DataFormatAttribute.
        /// </summary>
        /// <param name="numberStyles">If the type of the field/property implements a Parse method taking NumberStyles, this will be passed to it.</param>
        public DataFormatAttribute(NumberStyles numberStyles)
        {
            NumberStyles = numberStyles;
        }

        /// <summary>
        /// Initializes a new DataFormatAttribute.
        /// </summary>
        /// <param name="numberStyles">If the type of the field/property implements a Parse method taking NumberStyles, this will be passed to it.</param>
        /// <param name="format">A format to use when serializing objects supporting IFormattable, and used when deserializing objects with a compatible ParseExact method.</param>
        public DataFormatAttribute(NumberStyles numberStyles, string format)
        {
            NumberStyles = numberStyles;
            Format = format;
        }

        /// <summary>
        /// Initializes a new DataFormatAttribute.
        /// </summary>
        /// <param name="dateTimeStyles">If the type of the field/property implements a Parse method taking DateTimeStyles, this will be passed to it.</param>
        public DataFormatAttribute(DateTimeStyles dateTimeStyles)
        {
            DateTimeStyles = dateTimeStyles;
        }

        /// <summary>
        /// Initializes a new DataFormatAttribute.
        /// </summary>
        /// <param name="dateTimeStyles">If the type of the field/property implements a Parse method taking DateTimeStyles, this will be passed to it.</param>
        /// <param name="format">A format to use when serializing objects supporting IFormattable, and used when deserializing objects with a compatible ParseExact method.</param>
        public DataFormatAttribute(DateTimeStyles dateTimeStyles, string format)
        {
            DateTimeStyles = dateTimeStyles;
            Format = format;
        }
    }
}
