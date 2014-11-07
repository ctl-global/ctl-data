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
        public void WriteFixed1()
        {
            string str = Formats.Fixed.ToString(new[]
            {
                new FixedObject1 { Foo = 1234, Bar = "baz" },
                new FixedObject1 { Foo = 567890, Bar = "zab" }
            });

            Assert.Equal(str, "1234      baz\r\n567890    zab\r\n");
        }

        [Fact]
        public void WriteFixed2()
        {
            StringWriter sw = new StringWriter();
            FixedWriter<FixedObject1> fw = new FixedWriter<FixedObject1>(sw, writeNewLines: false);

            fw.WriteObjects(new[]
            {
                new FixedObject1 { Foo = 1234, Bar = "baz" },
                new FixedObject1 { Foo = 567890, Bar = "zab" }
            });
            fw.Close();

            string str = sw.ToString();

            Assert.Equal(str, "1234      baz567890    zab");
        }

        [Fact]
        public void WriteFixed3()
        {
            string str = Formats.Fixed.ToString(new[]
            {
                new FixedObject2 { Foo = 1234, Bar = "baz" },
                new FixedObject2 { Foo = 567890, Bar = "zab" }
            });

            Assert.Equal(str, "1234      baz   \r\n567890    zab   \r\n");
        }

        [Fact]
        public void WriteFixed4()
        {
            StringWriter sw = new StringWriter();
            FixedWriter<FixedObject2> fw = new FixedWriter<FixedObject2>(sw, writeNewLines: false);

            fw.WriteObjects(new[]
            {
                new FixedObject2 { Foo = 1234, Bar = "baz" },
                new FixedObject2 { Foo = 567890, Bar = "zab" }
            });
            fw.Close();

            string str = sw.ToString();

            Assert.Equal(str, "1234      baz   567890    zab   ");
        }

        sealed class FixedObject1
        {
            [Fixed(1, 10)]
            public int Foo { get; set; }

            [Fixed(11, 3)]
            public string Bar { get; set; }
        }

        [Fixed(16)]
        sealed class FixedObject2
        {
            [Fixed(1, 10)]
            public int Foo { get; set; }

            [Fixed(11, 3)]
            public string Bar { get; set; }
        }
    }
}
