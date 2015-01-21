using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
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
        public void ReadCsvObject1()
        {
            Compare(() => new CsvReader<CsvObject1>(new StringReader("Foo,raB,zaB,Enum\n3,56.7,890,Foo\n"), validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.Equal(v.Value.Foo, 3);
                Assert.Equal(v.Value.Bar, 56.7f);
                Assert.Equal(v.Value.Baz, "890");
                Assert.Equal(v.Value.Enum, TestEnum.Foo);
            });
        }

        [Fact]
        public void ReadCsvObject2()
        {
            Compare(() => new CsvReader<CsvObject1>(new StringReader("Foo,Bar,Baz,Enum,OptionalFormatted\n1234,56.7,890,Foo\ns,t,901\n3456,78.9,12,Bar,9876.000")), values =>
            {
                Assert.Equal(values.Length, 3);

                var v = values[0];

                Assert.Equal(v.Value.Foo, 1234);
                Assert.Equal(v.Value.Bar, 56.7f);
                Assert.Equal(v.Value.Baz, "890");
                Assert.Equal(v.Value.OptionalFormatted, null);

                v = values[1];

                Assert.IsType<AggregateException>(v.Exception);
                Assert.Throws<AggregateException>(() => v.Value);

                AggregateException ae = (AggregateException)v.Exception;

                Assert.Equal(ae.InnerExceptions.Count, 2);

                Assert.IsType<SerializationException>(ae.InnerExceptions[0]);
                SerializationException se = (SerializationException)ae.InnerExceptions[0];
                Assert.Equal(se.LineNumber, 3);
                Assert.Equal(se.ColumnNumber, 1);
                Assert.Equal(se.MemberName, "Foo");

                Assert.IsType<SerializationException>(ae.InnerExceptions[1]);
                se = (SerializationException)ae.InnerExceptions[1];
                Assert.Equal(se.LineNumber, 3);
                Assert.Equal(se.ColumnNumber, 3);
                Assert.Equal(se.MemberName, "Bar");

                v = values[2];

                Assert.Equal(v.Value.Foo, 3456);
                Assert.Equal(v.Value.Bar, 78.9f);
                Assert.Equal(v.Value.Baz, "12");
                Assert.Equal(v.Value.OptionalFormatted, 9876);
            });
        }

        [Fact]
        public void ReadCsvObject3()
        {
            Compare(() => new CsvReader<CsvObject1>(new StringReader("56.7,890,Foo,3\n"), readHeader: false, validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.Equal(v.Value.Foo, 3);
                Assert.Equal(v.Value.Bar, 56.7f);
                Assert.Equal(v.Value.Baz, "890");
                Assert.Equal(v.Value.Enum, TestEnum.Foo);
            });
        }

        [Fact]
        public void ReadCsvObject4()
        {
            Compare(() => new CsvReader<CsvObject1>(new StringReader("Foo,Bar,,Enum\n3,56.7,890,Foo\n"), validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.Equal(v.Value.Foo, 3);
                Assert.Equal(v.Value.Bar, 56.7f);
                Assert.Equal(v.Value.Baz, null);
                Assert.Equal(v.Value.Enum, TestEnum.Foo);
            });
        }

        [Fact]
        public void ReadCsvObject5()
        {
            Compare(() => new CsvReader<CsvObject3>(new StringReader("1,2,3,4,5,6,7\n"), readHeader: false, validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.Equal(4, v.Value.Foo);
                Assert.Equal(6, v.Value.Bar);
            });
        }

        [Fact]
        public void ReadCsvValidate1()
        {
            Compare(() => new CsvReader<CsvObject1>(new StringReader("Foo,Bar,Baz,Enum\n1234,56.7,890,Foo\n"), validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.IsType<AggregateException>(v.Exception);
                Assert.Throws<AggregateException>(() => v.Value);

                AggregateException ae = (AggregateException)v.Exception;

                Assert.Equal(ae.InnerExceptions.Count, 1);

                Assert.IsType<ValidationException>(ae.InnerExceptions[0]);
            });
        }

        [Fact]
        public void ReadCsvValidate2()
        {
            Compare(() => new CsvReader<CsvObject2>(new StringReader("Foo\n1\n"), validate: true), values =>
            {
                Assert.Equal(values.Length, 1);

                var v = values[0];

                Assert.IsType<AggregateException>(v.Exception);
                Assert.Throws<AggregateException>(() => v.Value);

                AggregateException ae = (AggregateException)v.Exception;

                Assert.Equal(ae.InnerExceptions.Count, 1);
                Assert.IsType<ValidationException>(ae.InnerExceptions[0]);

                ValidationException ve = (ValidationException)ae.InnerExceptions[0];

                Assert.Equal(ve.Errors.Count(), 1);
                Assert.Equal(ve.Errors.First().MemberNames.SingleOrDefault(), "Bar");
            });
        }

        [Fact]
        public void ReadCsvUnvalidatedColumns()
        {
            var reader = new CsvReader<CsvObject1>(new StringReader("Foo,Bar,Baz,Enum\n1234,56.7,890,Foo"));

            ObjectValue<CsvObject1> x = reader.AsEnumerable().Single();

            Assert.Equal("1234", x.GetValueForMember(y => y.Foo).Value);
            Assert.Equal("56.7", x.GetValueForMember(y => y.Bar).Value);
            Assert.Equal("890", x.GetValueForMember(y => y.Baz).Value);
            Assert.Equal("Foo", x.GetValueForMember(y => y.Enum).Value);
            Assert.Null(x.GetValueForMember(y => y.OptionalFormatted));
        }

        static void Compare<T>(Func<CsvReader<T>> getReader, Action<ObjectValue<T>[]> testFunc)
        {
            var values = getReader().AsEnumerable().ToArray();
            testFunc(values);

            values = getReader().AsAsyncEnumerable().ToArray().Result;
            testFunc(values);
        }

        sealed class CsvObject1
        {
            [Column(Order = 3), Range(0, 5)]
            public int Foo { get; set; }

            [Column(Order = 0), AdditionalNames("Bar", "raB")]
            public float Bar;

            [Column("Baz", Order = 1), AdditionalNames("zaB")]
            public string Baz;

            [Column(Order = 2)]
            public TestEnum Enum { get; set; }

            [Column(Order = 4), DataFormat(NumberStyles.Float)]
            public int? OptionalFormatted { get; set; }
        }

        sealed class CsvObject2
        {
            public int Foo { get; set; }

            [Required]
            public int Bar { get; set; }
        }

        sealed class CsvObject3
        {
            [Position(5)]
            public int Bar { get; set; }

            [Position(3)]
            public int Foo { get; set; }
        }
    }
}
