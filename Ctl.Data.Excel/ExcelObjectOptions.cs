using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data.Excel
{
    public class ExcelObjectOptions
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

        public ExcelObjectOptions()
        {
            FormatProvider = null;
            ReadHeader = true;
        }
    }
}
