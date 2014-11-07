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
    /// Specifies the input/output width and position when using a fixed-width format.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class FixedAttribute : Attribute
    {
        /// <summary>
        /// The 1-based index of the column of data when using a fixed-width format.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// The width of data when using a fixed-width format.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Initializes a new FixedAttribute.
        /// </summary>
        /// <param name="width">The data's width when using a fixed-width format.</param>
        public FixedAttribute(int width)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException("width", "width must be greater than zero.");

            Width = width;
            Position = -1;
        }

        /// <summary>
        /// Initializes a new FixedAttribute.
        /// </summary>
        /// <param name="position">The 1-based index of the column of data when using a fixed-width format.</param>
        /// <param name="width">The data's width when using a fixed-width format.</param>
        public FixedAttribute(int position, int width)
        {
            if (position <= 0) throw new ArgumentOutOfRangeException("position", "position must be greater than zero.");
            if (width <= 0) throw new ArgumentOutOfRangeException("width", "width must be greater than zero.");

            Width = width;
            Position = position;
        }
    }
}
