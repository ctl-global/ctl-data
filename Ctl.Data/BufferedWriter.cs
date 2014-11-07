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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Provides a base for buffered data writers.
    /// </summary>
    public class BufferedWriter
    {
        const int BufferLength = 512;

        readonly TextWriter writer;
        readonly StringBuilder sb = new StringBuilder(BufferLength * 3 / 2);

        internal StringBuilder StringBuilder { get { return sb; } }

        internal BufferedWriter(TextWriter writer)
        {
            Debug.Assert(writer != null);
            this.writer = writer;
        }

        internal void WeakFlush()
        {
            if (sb.Length > BufferLength)
            {
                writer.Write(sb.ToString());
                sb.Clear();
            }
        }

        internal void StrongFlush()
        {
            if (sb.Length > 0)
            {
                writer.Write(sb.ToString());
                sb.Clear();
            }
        }

        internal void FullFlush()
        {
            StrongFlush();
            writer.Flush();
        }

        static readonly Task finishedTask = Task.FromResult(true);

        internal Task WeakFlushAsync()
        {
            if (sb.Length > BufferLength)
            {
                string str = sb.ToString();

                sb.Clear();
                return writer.WriteAsync(str);
            }

            return finishedTask;
        }

        internal Task StrongFlushAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (sb.Length > 0)
            {
                string str = sb.ToString();

                sb.Clear();
                return writer.WriteAsync(str);
            }

            return finishedTask;
        }

        internal async Task FullFlushAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (sb.Length > 0)
            {
                string str = sb.ToString();

                sb.Clear();
                await writer.WriteAsync(str).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }
}
