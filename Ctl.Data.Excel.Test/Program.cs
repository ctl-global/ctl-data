using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data.Excel.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ExcelOptions opts = new ExcelOptions
            {
                TrimWhitespace = true,
                ReadFormatted = true,
                UnformattedFormat = CultureInfo.InvariantCulture
            };

            using (ExcelPackage pkg = new ExcelPackage(new FileInfo("test.xlsx")))
            {
                var strings = from ws in pkg.Workbook.Worksheets
                              from rv in new Ctl.Data.Excel.ExcelReader(ws, opts).AsEnumerable()
                              from cv in rv
                              select cv.Value;

                foreach (var v in strings)
                {
                    Console.WriteLine(v);
                }
            }
        }
    }
}
