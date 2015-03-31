using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// A set of options for reading CSV.
    /// </summary>
    public class CsvOptions
    {
        /// <summary>
        /// The column separator.
        /// </summary>
        public char Separator { get; set; }

        /// <summary>
        /// If true, unescaped quotes in the middle of a quoted value are assumed to be part of the value. Otherwise, require strict escaping.
        /// </summary>
        public bool ParseMidQuotes { get; set; }

        /// <summary>
        /// The internal buffer length to use. Higher values will trade memory for performance.
        /// </summary>
        public int BufferLength { get; set; }

        public CsvOptions()
        {
            Separator = ',';
            BufferLength = 4096;
        }
    }

    /// <summary>
    /// A set of options for reading objects from CSV.
    /// </summary>
    public class CsvObjectOptions : CsvOptions
    {
        /// <summary>
        /// A format provider used to deserialize objects.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// If true, read a header. Otherwise, use column indexes.
        /// </summary>
        public bool ReadHeader { get; set; }

        /// <summary>
        /// A comparer used to match header values to property names.
        /// </summary>
        public IEqualityComparer<string> HeaderComparer { get; set; }

        /// <summary>
        /// If true, validate objects to conform to their data annotations.
        /// </summary>
        public bool Validate { get; set; }

        public CsvObjectOptions()
        {
            FormatProvider = null;
            ReadHeader = true;
        }
    }
}
