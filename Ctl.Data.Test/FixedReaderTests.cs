using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ctl.Data.Test
{
    public partial class Tests
    {
        [Fact]
        public void ReadFixed1()
        {
            CompareFixed("abbcccdddd\neffggghhhh", new[]
            {
                new FixedPosition(1, 1),
                new FixedPosition(2, 2),
                new FixedPosition(4, 3),
                new FixedPosition(7, 4)
            }, null, true, new[]
            {
                new ColumnValue[]
                {
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 1,
                        Value = "a"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 2,
                        Value = "bb"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 4,
                        Value = "ccc"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 7,
                        Value = "dddd"
                    }
                },
                new ColumnValue[]
                {
                    new ColumnValue
                    {
                        LineNumber = 2,
                        ColumnNumber = 1,
                        Value = "e"
                    },
                    new ColumnValue
                    {
                        LineNumber = 2,
                        ColumnNumber = 2,
                        Value = "ff"
                    },
                    new ColumnValue
                    {
                        LineNumber = 2,
                        ColumnNumber = 4,
                        Value = "ggg"
                    },
                    new ColumnValue
                    {
                        LineNumber = 2,
                        ColumnNumber = 7,
                        Value = "hhhh"
                    }
                }
            });
        }

        [Fact]
        public void ReadFixed2()
        {
            CompareFixed("abbcccddddeffggghhhh", new[]
            {
                new FixedPosition(1, 1),
                new FixedPosition(2, 2),
                new FixedPosition(4, 3),
                new FixedPosition(7, 4)
            }, null, false, new[]
            {
                new ColumnValue[]
                {
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 1,
                        Value = "a"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 2,
                        Value = "bb"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 4,
                        Value = "ccc"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 7,
                        Value = "dddd"
                    }
                },
                new ColumnValue[]
                {
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 11,
                        Value = "e"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 12,
                        Value = "ff"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 14,
                        Value = "ggg"
                    },
                    new ColumnValue
                    {
                        LineNumber = 1,
                        ColumnNumber = 17,
                        Value = "hhhh"
                    }
                }
            });
        }
    }
}
