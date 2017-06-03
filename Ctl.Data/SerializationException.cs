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
    /// An exception resulting from deserializing a value.
    /// </summary>
#if NET45 || NETSTANDARD2_0
    [Serializable]
#endif
    public class SerializationException : ParseException
    {
        /// <summary>
        /// The member of the object which threw an exception during deserialization.
        /// </summary>
        public string MemberName { get; private set; }

        /// <summary>
        /// The serialized value which caused the exception.
        /// </summary>
        public string InvalidValue { get; private set; }

        /// <summary>
        /// Instantiates a new SerializationException.
        /// </summary>
        public SerializationException()
        {
        }

        /// <summary>
        /// Instantiates a new SerializationException.
        /// </summary>
        /// <param name="message">The exception's message.</param>
        public SerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiates a new SerializationException.
        /// </summary>
        /// <param name="lineNumber">The 1-based line number of the invalid value in the input stream.</param>
        /// <param name="columnNumber">The 1-based column number of the invalid value in the input stream.</param>
        /// <param name="memberName">The member of the object which threw an exception during deserialization.</param>
        /// <param name="invalidValue">The serialized value which caused the exception.</param>
        /// <param name="innerException">The exception which occurred during deserialization.</param>
        public SerializationException(long lineNumber, long columnNumber, string memberName, string invalidValue, Exception innerException)
            : base(string.Format("An error occurred deserializing member {0} at {1}:{2}. See InnerException for details.", memberName, lineNumber, columnNumber), lineNumber, columnNumber, innerException)
        {
            MemberName = memberName;
            InvalidValue = invalidValue;
        }

#if NET45 || NETSTANDARD2_0
        /// <summary>
        /// Instantiates a new SerializationException from a serialized instance.
        /// </summary>
        /// <param name="info">Serialization info to deserialize from.</param>
        /// <param name="context">A context to use while deserializing.</param>
        protected SerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            MemberName = info.GetString("MemberName");
            InvalidValue = info.GetString("InvalidValue");
        }

        /// <summary>
        /// Serializes the SerializationException for remoting.
        /// </summary>
        /// <param name="info">Serialization info to store data to.</param>
        /// <param name="context">A context to use while serializing.</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
            info.AddValue("MemberName", MemberName, typeof(string));
            info.AddValue("InvalidValue", InvalidValue, typeof(string));
        }
#endif
    }
}
