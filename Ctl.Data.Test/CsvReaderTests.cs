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
        [Fact]
        public void ReadCsvEmptyValues1()
        {
            CompareCsv("", false, new ColumnValue[0][]);
        }

        [Fact]
        public void ReadCsvEmptyValues2()
        {
            CompareCsv(",", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null },
                    new ColumnValue { LineNumber = 1, ColumnNumber = 2, Value = null }
                }
            });
        }

        [Fact]
        public void ReadCsvEmptyValues3()
        {
            CompareCsv(",\n,", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null },
                    new ColumnValue { LineNumber = 1, ColumnNumber = 2, Value = null }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 2, ColumnNumber = 1, Value = null },
                    new ColumnValue { LineNumber = 2, ColumnNumber = 2, Value = null }
                }
            });
        }

        [Fact]
        public void ReadCsvTrailingNewLine1()
        {
            CompareCsv(",\r\n", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null },
                    new ColumnValue { LineNumber = 1, ColumnNumber = 2, Value = null }
                }
            });
        }
        

        [Fact]
        public void ReadCsvTrailingNewLine2()
        {
            CompareCsv(",\"asdf\"\r\n", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null },
                    new ColumnValue { LineNumber = 1, ColumnNumber = 2, Value = "asdf" }
                }
            });
        }

        [Fact]
        public void ReadCsvQuotedValues1()
        {
            CompareCsv("\"\"", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null },
                }
            });
        }

        [Fact]
        public void ReadCsvQuotedValues2()
        {
            CompareCsv("\",\n\",\"\"\n", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = ",\n" },
                    new ColumnValue { LineNumber = 2, ColumnNumber = 3, Value = null },
                }
            });
        }

        [Fact]
        public void ReadCsvQuotedValues3()
        {
            CompareCsv("\"\"\nfoo\"\n\"bar\",ba\"z", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = null }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 2, ColumnNumber = 1, Value = "foo\"" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 3, ColumnNumber = 1, Value = "bar" },
                    new ColumnValue { LineNumber = 3, ColumnNumber = 7, Value = "ba\"z" }
                }
            });
        }

        [Fact]
        public void ReadCsvQuotedValues4()
        {
            CompareCsv("\"a\rb\nc\r\nd\n\re\"\"f\",g", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = "a\rb\nc\r\nd\n\re\"f" },
                    new ColumnValue { LineNumber = 5, ColumnNumber = 7, Value = "g" }
                }
            });
        }

        [Fact]
        public void ReadCsvQuotedValues5()
        {
            CompareCsv("\"foo\"bar\"", true, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = "foo\"bar" }
                }
            });
        }

        [Fact]
        public void ReadCsvNewLines()
        {
            CompareCsv("\"a\"\r\"b\"\n\"c\"\r\n\"d\"\n\re\rf\ng\r\nh\n\ri", false, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = "a" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 2, ColumnNumber = 1, Value = "b" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 3, ColumnNumber = 1, Value = "c" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 4, ColumnNumber = 1, Value = "d" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 5, ColumnNumber = 1, Value = "e" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 6, ColumnNumber = 1, Value = "f" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 7, ColumnNumber = 1, Value = "g" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 8, ColumnNumber = 1, Value = "h" }
                },
                new[]
                {
                    new ColumnValue { LineNumber = 9, ColumnNumber = 1, Value = "i" }
                }
            });
        }

        [Fact]
        public void ReadCsvBadMidQuote()
        {
            ParseException ex = Assert.Throws<ParseException>(() =>
            {
                new CsvReader(new StringReader("\"foo\"bar\"")).AsEnumerable().ToArray();
            });

            Assert.Equal(ex.LineNumber, 1);
            Assert.Equal(ex.ColumnNumber, 5);
        }

        [Fact]
        public void ReadCsvUnfinishedQuote()
        {
            ParseException ex = Assert.Throws<ParseException>(() =>
            {
                new CsvReader(new StringReader("\"foo")).AsEnumerable().ToArray();
            });

            Assert.Equal(ex.LineNumber, 1);
            Assert.Equal(ex.ColumnNumber, 5);
        }

        [Fact]
        public void ReadCsvTrimmed()
        {
            CompareCsv("\"foo bar   \"", new CsvOptions { TrimWhitespace = true }, new[]
            {
                new[]
                {
                    new ColumnValue { LineNumber = 1, ColumnNumber = 1, Value = "foo bar" }
                }
            });
        }
    }
}
