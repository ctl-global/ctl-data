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
    /// Encapsulates utility methods for quickly using a format without writing much boilerplate code.
    /// </summary>
    public static class Formats
    {
        /// <summary>
        /// Encapsulates utility methods for quickly using CSV without writing much boilerplate code.
        /// </summary>
        public static readonly CsvFormat Csv = new CsvFormat();

        /// <summary>
        /// Encapsulates utility methods for quickly using fixed width without writing much boilerplate code.
        /// </summary>
        public static readonly FixedFormat Fixed = new FixedFormat();

        /// <summary>
        /// Encapsulates utility methods for quickly using a format without writing much boilerplate code.
        /// </summary>
        public abstract class Format
        {
            protected Format() { }

            protected abstract IDataReader OpenRead(TextReader reader);

            protected abstract IDataReader<T> OpenRead<T>(TextReader reader, IFormatProvider formatProvider, bool validate);

            protected abstract IDataWriter OpenWrite(TextWriter writer);

            protected abstract IDataWriter<T> OpenWrite<T>(TextWriter writer, IFormatProvider formatProvider, bool validate);

            public IEnumerable<RowValue> ReadRecords(string filePath)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                using (StreamReader sr = OpenSyncReader(filePath))
                {
                    IDataReader reader = OpenRead(sr);
                    while (reader.Read())
                    {
                        yield return reader.CurrentRow;
                    }
                }
            }

            public IEnumerable<RowValue> ReadRecords(string filePath, Encoding encoding)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                using (StreamReader sr = OpenSyncReader(filePath, encoding))
                {
                    IDataReader reader = OpenRead(sr);
                    while (reader.Read())
                    {
                        yield return reader.CurrentRow;
                    }
                }
            }

            public IEnumerable<RowValue> ReadRecords(TextReader textReader)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return OpenRead(textReader).AsEnumerable();
            }

            public IEnumerable<ObjectValue<T>> ReadObjectValues<T>(string filePath, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                using (StreamReader sr = OpenSyncReader(filePath))
                {
                    IDataReader<T> reader = OpenRead<T>(sr, formatProvider, validate);
                    while (reader.Read())
                    {
                        yield return reader.CurrentObject;
                    }
                }
            }

            public IEnumerable<ObjectValue<T>> ReadObjectValues<T>(string filePath, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                using (StreamReader sr = OpenSyncReader(filePath, encoding))
                {
                    IDataReader<T> reader = OpenRead<T>(sr, formatProvider, validate);
                    while (reader.Read())
                    {
                        yield return reader.CurrentObject;
                    }
                }
            }

            public IEnumerable<ObjectValue<T>> ReadObjectValues<T>(TextReader textReader, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return OpenRead<T>(textReader, formatProvider, validate).AsEnumerable();
            }

            public IEnumerable<T> ReadObjects<T>(string filePath, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return ReadObjectValues<T>(filePath, formatProvider, validate).Select(x => x.Value);
            }

            public IEnumerable<T> ReadObjects<T>(string filePath, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return ReadObjectValues<T>(filePath, encoding, formatProvider, validate).Select(x => x.Value);
            }

            public IEnumerable<T> ReadObjects<T>(TextReader textReader, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return ReadObjectValues<T>(textReader, formatProvider, validate).Select(x => x.Value);
            }

            public IAsyncEnumerable<RowValue> ReadRecordsAsync(string filePath)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return AsyncEnumerable.Using(() => OpenAsyncReader(filePath), sr => OpenRead(sr).AsAsyncEnumerable());
            }

            public IAsyncEnumerable<RowValue> ReadRecordsAsync(string filePath, Encoding encoding)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return AsyncEnumerable.Using(() => OpenAsyncReader(filePath, encoding), sr => OpenRead(sr).AsAsyncEnumerable());
            }

            public IAsyncEnumerable<RowValue> ReadRecordsAsync(TextReader textReader)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return OpenRead(textReader).AsAsyncEnumerable();
            }

            public IAsyncEnumerable<ObjectValue<T>> ReadObjectValuesAsync<T>(string filePath, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return AsyncEnumerable.Using(() => OpenAsyncReader(filePath), sr => OpenRead<T>(sr, formatProvider, validate).AsAsyncEnumerable());
            }

            public IAsyncEnumerable<ObjectValue<T>> ReadObjectValuesAsync<T>(string filePath, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return AsyncEnumerable.Using(() => OpenAsyncReader(filePath, encoding), sr => OpenRead<T>(sr, formatProvider, validate).AsAsyncEnumerable());
            }

            public IAsyncEnumerable<ObjectValue<T>> ReadObjectValuesAsync<T>(TextReader textReader, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return OpenRead<T>(textReader, formatProvider, validate).AsAsyncEnumerable();
            }

            public IAsyncEnumerable<T> ReadObjectsAsync<T>(string filePath, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return ReadObjectValuesAsync<T>(filePath, formatProvider, validate).Select(x => x.Value);
            }

            public IAsyncEnumerable<T> ReadObjectsAsync<T>(string filePath, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");

                return ReadObjectValuesAsync<T>(filePath, encoding, formatProvider, validate).Select(x => x.Value);
            }

            public IAsyncEnumerable<T> ReadObjectsAsync<T>(TextReader textReader, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (textReader == null) throw new ArgumentNullException("textReader");

                return ReadObjectValuesAsync<T>(textReader, formatProvider, validate).Select(x => x.Value);
            }

            static StreamReader OpenSyncReader(string filePath)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                return new StreamReader(fs);
            }

            static StreamReader OpenSyncReader(string filePath, Encoding encoding)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                return new StreamReader(fs, encoding);
            }

            static StreamReader OpenAsyncReader(string filePath)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return new StreamReader(fs);
            }

            static StreamReader OpenAsyncReader(string filePath, Encoding encoding)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return new StreamReader(fs, encoding);
            }

            public void Write(string filePath, IEnumerable<IEnumerable<string>> records)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenSyncWriter(filePath))
                {
                    Write(sw, records);
                }
            }

            public void Write(string filePath, Encoding encoding, IEnumerable<IEnumerable<string>> records)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenSyncWriter(filePath, encoding))
                {
                    Write(sw, records);
                }
            }

            public void Write(TextWriter textWriter, IEnumerable<IEnumerable<string>> records)
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (records == null) throw new ArgumentNullException("records");

                IDataWriter writer = OpenWrite(textWriter);
                writer.WriteRows(records);
                writer.Flush();
            }

            public async Task WriteAsync(string filePath, IEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenAsyncWriter(filePath))
                {
                    await WriteAsync(sw, records, token).ConfigureAwait(false);
                }
            }

            public async Task WriteAsync(string filePath, Encoding encoding, IEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenAsyncWriter(filePath, encoding))
                {
                    await WriteAsync(sw, records, token).ConfigureAwait(false);
                }
            }

            public async Task WriteAsync(TextWriter textWriter, IEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (records == null) throw new ArgumentNullException("records");

                IDataWriter dw = OpenWrite(textWriter);

                await dw.WriteRowsAsync(records, token).ConfigureAwait(false);
                await dw.FlushAsync(token).ConfigureAwait(false);
            }

            public async Task WriteAsync(string filePath, IAsyncEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenAsyncWriter(filePath))
                {
                    await WriteAsync(sw, records, token).ConfigureAwait(false);
                }
            }

            public async Task WriteAsync(string filePath, Encoding encoding, IAsyncEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (records == null) throw new ArgumentNullException("records");

                using (StreamWriter sw = OpenAsyncWriter(filePath, encoding))
                {
                    await WriteAsync(sw, records, token).ConfigureAwait(false);
                }
            }

            public async Task WriteAsync(TextWriter textWriter, IAsyncEnumerable<IEnumerable<string>> records, CancellationToken token = default(CancellationToken))
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (records == null) throw new ArgumentNullException("records");

                IDataWriter dw = OpenWrite(textWriter);

                await dw.WriteRowsAsync(records, token).ConfigureAwait(false);
                await dw.FlushAsync(token).ConfigureAwait(false);
            }

            public void Write<T>(string filePath, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenSyncWriter(filePath))
                {
                    Write(sw, items, formatProvider, validate);
                }
            }

            public void Write<T>(string filePath, Encoding encoding, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenSyncWriter(filePath, encoding))
                {
                    Write(sw, items, formatProvider, validate);
                }
            }

            public void Write<T>(TextWriter textWriter, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (items == null) throw new ArgumentNullException("items");

                IDataWriter<T> dw = OpenWrite<T>(textWriter, formatProvider, validate);

                dw.WriteObjects(items);
                dw.Flush();
            }

            public async Task WriteAsync<T>(string filePath, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenAsyncWriter(filePath))
                {
                    await WriteAsync(sw, items, formatProvider, validate, token);
                }
            }

            public async Task WriteAsync<T>(string filePath, Encoding encoding, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenAsyncWriter(filePath, encoding))
                {
                    await WriteAsync(sw, items, formatProvider, validate, token);
                }
            }

            public async Task WriteAsync<T>(TextWriter textWriter, IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (items == null) throw new ArgumentNullException("items");

                IDataWriter<T> dw = OpenWrite<T>(textWriter, formatProvider, validate);

                await dw.WriteObjectsAsync(items, token).ConfigureAwait(false);
                await dw.FlushAsync(token).ConfigureAwait(false);
            }

            public async Task WriteAsync<T>(string filePath, IAsyncEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenAsyncWriter(filePath))
                {
                    await WriteAsync(sw, items, formatProvider, validate, token);
                }
            }

            public async Task WriteAsync<T>(string filePath, Encoding encoding, IAsyncEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (filePath == null) throw new ArgumentNullException("filePath");
                if (items == null) throw new ArgumentNullException("items");

                using (StreamWriter sw = OpenAsyncWriter(filePath, encoding))
                {
                    await WriteAsync(sw, items, formatProvider, validate, token);
                }
            }

            public async Task WriteAsync<T>(TextWriter textWriter, IAsyncEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (textWriter == null) throw new ArgumentNullException("textWriter");
                if (items == null) throw new ArgumentNullException("items");

                IDataWriter<T> dw = OpenWrite<T>(textWriter, formatProvider, validate);

                await dw.WriteObjectsAsync(items, token).ConfigureAwait(false);
                await dw.FlushAsync(token).ConfigureAwait(false);
            }

            static StreamWriter OpenSyncWriter(string filePath)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
                return new StreamWriter(fs);
            }

            static StreamWriter OpenSyncWriter(string filePath, Encoding encoding)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
                return new StreamWriter(fs, encoding);
            }

            static StreamWriter OpenAsyncWriter(string filePath)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return new StreamWriter(fs);
            }

            static StreamWriter OpenAsyncWriter(string filePath, Encoding encoding)
            {
                Debug.Assert(filePath != null);

                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return new StreamWriter(fs, encoding);
            }

            public MemoryStream ToStream<T>(IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (items == null) throw new ArgumentNullException("items");

                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms);

                Write(sw, items, formatProvider, validate);
                sw.Flush();

                ms.Position = 0;
                return ms;
            }

            public MemoryStream ToStream<T>(IEnumerable<T> items, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (items == null) throw new ArgumentNullException("items");

                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms, encoding);

                Write(sw, items, formatProvider, validate);
                sw.Flush();

                ms.Position = 0;
                return ms;
            }

            public async Task<MemoryStream> ToStreamAsync<T>(IAsyncEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (items == null) throw new ArgumentNullException("items");

                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms);

                await WriteAsync(sw, items, formatProvider, validate, token).ConfigureAwait(false);
                sw.Flush();

                ms.Position = 0;
                return ms;
            }

            public async Task<MemoryStream> ToStreamAsync<T>(IAsyncEnumerable<T> items, Encoding encoding, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (items == null) throw new ArgumentNullException("items");

                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms, encoding);

                await WriteAsync(sw, items, formatProvider, validate, token).ConfigureAwait(false);
                sw.Flush();

                ms.Position = 0;
                return ms;
            }

            public string ToString<T>(IEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false)
            {
                if (items == null) throw new ArgumentNullException("items");

                StringWriter sw = new StringWriter();

                Write(sw, items, formatProvider, validate);

                return sw.ToString();
            }

            public async Task<string> ToStringAsync<T>(IAsyncEnumerable<T> items, IFormatProvider formatProvider = null, bool validate = false, CancellationToken token = default(CancellationToken))
            {
                if (items == null) throw new ArgumentNullException("items");

                StringWriter sw = new StringWriter();

                await WriteAsync(sw, items, formatProvider, validate).ConfigureAwait(false);

                return sw.ToString();
            }
        }

        public sealed class CsvFormat : Format
        {
            internal CsvFormat() { }

            protected override IDataReader OpenRead(TextReader reader)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                return new CsvReader(reader);
            }

            protected override IDataReader<T> OpenRead<T>(TextReader reader, IFormatProvider formatProvider, bool validate)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                return new CsvReader<T>(reader, formatProvider, validate: validate);
            }

            protected override IDataWriter OpenWrite(TextWriter writer)
            {
                if (writer == null) throw new ArgumentNullException("writer");
                return new CsvWriter(writer);
            }

            protected override IDataWriter<T> OpenWrite<T>(TextWriter writer, IFormatProvider formatProvider, bool validate)
            {
                if (writer == null) throw new ArgumentNullException("writer");
                return new CsvWriter<T>(writer, formatProvider, validate: validate);
            }
        }

        public sealed class FixedFormat : Format
        {
            internal FixedFormat() { }

            protected override IDataReader OpenRead(TextReader reader)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                throw new NotImplementedException();
            }

            protected override IDataReader<T> OpenRead<T>(TextReader reader, IFormatProvider formatProvider, bool validate)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                return new FixedReader<T>(reader, formatProvider, validate: validate);
            }

            protected override IDataWriter OpenWrite(TextWriter writer)
            {
                if (writer == null) throw new ArgumentNullException("writer");
                throw new NotImplementedException();
            }

            protected override IDataWriter<T> OpenWrite<T>(TextWriter writer, IFormatProvider formatProvider, bool validate)
            {
                if (writer == null) throw new ArgumentNullException("writer");
                return new FixedWriter<T>(writer, formatProvider: formatProvider, validate: validate);
            }
        }
    }
}
