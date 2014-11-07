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
        public void WriteCsvObject()
        {
            string str = Formats.Csv.ToString(new[]
            {
                new
                {
                    W = TestEnum.Bar,
                    X = "foo",
                    Y = 123,
                    Z = 1.23m,
                }
            });

            Assert.Equal(str, "W,X,Y,Z\r\nBar,foo,123,1.23\r\n");
        }

        [Fact]
        public void WriteCsvObjectValidated()
        {
            var obj = new CsvObject1{ Foo = 10 };

            ValidationException ex = Assert.Throws<ValidationException>(() => Formats.Csv.ToString(new[]
            {
                obj
            }, validate: true));

            Assert.Equal(ex.Object, obj);
            Assert.Equal(ex.LineNumber, 0);
            Assert.Equal(ex.ColumnNumber, 0);
            Assert.Equal(ex.Errors.Count(), 1);
        }

        enum TestEnum
        {
            Unspecified,
            Foo,
            Bar
        }
    }
}
