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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET45 || NETSTANDARD2_0
using System.Runtime.Serialization;
#endif

namespace Ctl.Data
{
    /// <summary>
    /// An exception thrown when an object doesn't pass validation.
    /// </summary>
#if NET45 || NETSTANDARD2_0
    [Serializable]
#endif
    public class ValidationException : ParseException
    {
        /// <summary>
        /// Specific errors associated with the object.
        /// </summary>
        public IEnumerable<ValidationResult> Errors { get; private set; }

        /// <summary>
        /// The object which contains validation errors.
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Instantiates a new ValidationException.
        /// </summary>
        public ValidationException()
        {
        }

        /// <summary>
        /// Instantiates a new ValidationException.
        /// </summary>
        /// <param name="errors">Validation errors which caused this exception.</param>
        /// <param name="obj">The object which failed validation.</param>
        /// <param name="lineNumber">The 1-based line number the serialized value originated from.</param>
        /// <param name="columnNumber">The 1-based column number the serialized value originated from.</param>
        public ValidationException(IEnumerable<ValidationResult> errors, object obj, long lineNumber, long columnNumber)
            : base("Validation errors occured.", lineNumber, columnNumber)
        {
            if (errors == null) throw new ArgumentNullException("errors");
            if (obj == null) throw new ArgumentNullException("obj");

            Errors = errors;
            Object = obj;
        }

#if NET45 || NETSTANDARD2_0
        /// <summary>
        /// Instantiates a new ValidationException from a serialized instance.
        /// </summary>
        /// <param name="info">Serialization info to deserialize from.</param>
        /// <param name="context">A context to use while deserializing.</param>
        protected ValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            var value = info.GetValue("Errors", typeof(List<SerializedValidationResult>));
            Errors = ((List<SerializedValidationResult>)value).Select(x => new ValidationResult(x.ErrorMessage, x.MemberNames)).ToArray();
        }

        /// <summary>
        /// Serializes the ValidationException for remoting.
        /// </summary>
        /// <param name="info">Serialization info to store data to.</param>
        /// <param name="context">A context to use while serializing.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
            info.AddValue("Errors", Errors.Select(x => new SerializedValidationResult { ErrorMessage = x.ErrorMessage, MemberNames = x.MemberNames.ToList() }).ToList(), typeof(List<SerializedValidationResult>));
        }

        [Serializable]
        sealed class SerializedValidationResult
        {
            public string ErrorMessage { get; set; }

            public List<string> MemberNames { get; set; }
        }
#endif
    }
}
