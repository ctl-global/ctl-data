using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ctl.Data.Test
{
    public partial class Tests
    {
        static void CompareCsv(string data, bool parseMidQuotes, ColumnValue[][] values)
        {
            CompareCsv(data, parseMidQuotes, 4096, values);
            CompareCsv(data, parseMidQuotes, 5, values);
            CompareCsv(data, parseMidQuotes, 4, values);
            CompareCsv(data, parseMidQuotes, 3, values);
        }

        static void CompareCsv(string data, bool parseMidQuotes, int bufferLength, ColumnValue[][] values)
        {
            CompareCsv(data, new CsvOptions
            {
                ParseMidQuotes = parseMidQuotes,
                BufferLength = bufferLength
            }, values);
        }

        static void CompareCsv(string data, CsvOptions opts, ColumnValue[][] values)
        {
            ColumnValue[][] readValues;

            // using Read.

            using (TextReader reader = new StringReader(data))
            {
                CsvReader csv = new CsvReader(reader, opts);
                readValues = csv.AsEnumerable().Select(x => x.ToArray()).ToArray();
            }

            Compare(readValues, values);

            // using ReadAsync + TryRead.

            using (TextReader reader = new StringReader(data))
            {
                CsvReader csv = new CsvReader(reader, opts);
                readValues = csv.AsAsyncEnumerable().Select(x => x.ToArray()).ToArray().Result;
            }

            Compare(readValues, values);
        }

        static void CompareFixed(string data, FixedPosition[] positions, int? recordWidth, bool readNewLines, ColumnValue[][] values)
        {
            ColumnValue[][] readValues;

            // using Read.

            using (TextReader reader = new StringReader(data))
            {
                FixedReader fr = new FixedReader(reader, positions, recordWidth, readNewLines);
                readValues = fr.AsEnumerable().Select(x => x.ToArray()).ToArray();
            }

            Compare(readValues, values);

            // using ReadAsync + TryRead.

            using (TextReader reader = new StringReader(data))
            {
                FixedReader fr = new FixedReader(reader, positions, recordWidth, readNewLines);
                readValues = fr.AsAsyncEnumerable().Select(x => x.ToArray()).ToArray().Result;
            }

            Compare(readValues, values);
        }

        static void Compare(ColumnValue[][] readValues, ColumnValue[][] constValues)
        {
            Assert.Equal(readValues.Length, constValues.Length);

            for (int i = 0; i < constValues.Length; ++i)
            {
                ColumnValue[] readRow = readValues[i];
                ColumnValue[] constRow = constValues[i];

                Assert.Equal(readRow.Length, constRow.Length);

                for (int j = 0; j < constRow.Length; ++j)
                {
                    ColumnValue readVal = readRow[j];
                    ColumnValue constVal = constRow[j];

                    Assert.Equal(readVal.Value, constVal.Value);
                    Assert.Equal(readVal.LineNumber, constVal.LineNumber);
                    Assert.Equal(readVal.ColumnNumber, constVal.ColumnNumber);
                }
            }
        }
    }
}
