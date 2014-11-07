using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ctl.Data.Test
{
    public partial class Tests
    {
        /// <summary>
        /// Everything is OK if valid.
        /// </summary>
        [Fact]
        void MemberValidation1()
        {
            TestIsValid<Validation1>("Value\n1");
        }

        /// <summary>
        /// Member attribute is called.
        /// </summary>
        [Fact]
        void MemberValidation2()
        {
            var res = TestIsInvalid<Validation1>("Value\n4");

            Assert.Equal(res.Length, 1);

            var err = res[0];
            Assert.Equal(err.MemberNames.Count(), 1);
            Assert.Contains("Value", err.MemberNames);
        }
        
        /// <summary>
        /// Everything is OK if valid.
        /// </summary>
        [Fact]
        void ClassValidation1()
        {
            TestIsValid<Validation2>("Value,Value2\n1,2");
        }
        
        /// <summary>
        /// Class attribute is called.
        /// </summary>
        [Fact]
        void ClassValidation2()
        {
            var res = TestIsInvalid<Validation2>("Value,Value2\n1,1");

            Assert.Equal(res.Length, 1);
        }
        
        /// <summary>
        /// Member attributes will be caught before class attributes.
        /// </summary>
        [Fact]
        void ClassValidation3()
        {
            var res = TestIsInvalid<Validation2>("Value,Value2\n0,2");

            Assert.Equal(res.Length, 1);

            var err = res[0];
            Assert.Equal(err.MemberNames.Count(), 1);
            Assert.Contains("Value", err.MemberNames);
        }

        /// <summary>
        /// Everything is OK if valid.
        /// </summary>
        [Fact]
        void Validatable1()
        {
            TestIsValid<Validation3>("Value,Value2\n2,3");
        }

        /// <summary>
        /// IValidatable is called.
        /// </summary>
        [Fact]
        void Validatable2()
        {
            var res = TestIsInvalid<Validation3>("Value,Value2\n3,4");

            Assert.Equal(res.Length, 1);
        }

        /// <summary>
        /// Member attributes will be caught before class attributes and IValidatable.
        /// </summary>
        [Fact]
        void Validatable3()
        {
            var res = TestIsInvalid<Validation3>("Value,Value2\n0,2");

            Assert.Equal(res.Length, 1);

            var err = res[0];
            Assert.Equal(err.MemberNames.Count(), 1);
            Assert.Contains("Value", err.MemberNames);
        }

        /// <summary>
        /// Class attributes will be caught before IValidatable.
        /// </summary>
        [Fact]
        void Validatable4()
        {
            var res = TestIsInvalid<Validation3>("Value,Value2\n1,3");

            Assert.Equal(res.Length, 1);

            var err = res[0];
            Assert.Equal(err.MemberNames.Count(), 0);
        }

        static T TestIsValid<T>(string data)
        {
            return Formats.Csv.ReadObjects<T>(new StringReader(data), validate: true).Single();
        }

        static ValidationResult[] TestIsInvalid<T>(string data)
        {
            return Assert.Throws<AggregateException>(() => Formats.Csv.ReadObjects<T>(new StringReader(data), validate: true).Single())
                .InnerExceptions.OfType<ValidationException>()
                .SelectMany(x => x.Errors)
                .ToArray();
        }
    }

    sealed class Validation1
    {
        [Range(1, 3)]
        public int Value { get; set; }
    }

    [Validation2Attribute]
    class Validation2
    {
        [Range(1, 3)]
        public int Value { get; set; }

        public int Value2 { get; set; }
    }

    sealed class Validation3 : Validation2, IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Value % 2 != 0)
            {
                yield return new ValidationResult("Field Value must be even.");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    sealed class Validation2Attribute : ValidationAttribute
    {
        public Validation2Attribute()
            : base("Field Value2 must be one greater than field Value")
        {
        }

        public override bool IsValid(object value)
        {
            Validation2 v = value as Validation2;
            Assert.NotNull(v);

            return v.Value2 == v.Value + 1;
        }
    }
}
