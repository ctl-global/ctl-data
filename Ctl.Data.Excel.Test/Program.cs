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
                ExcelWorksheet sheet = pkg.Workbook.Worksheets.First();
                ExcelAddressBase dim = sheet.Dimension;

                if (dim == null)
                {
                    return;
                }

                int start = dim.Start.Row + 6;

                if (start > dim.End.Row)
                {
                    return;
                }

                ExcelRange range = sheet.Cells[start, dim.Start.Column, dim.End.Row, dim.End.Column];
                
                var strings = from rv in new Ctl.Data.Excel.ExcelReader(range, opts).AsEnumerable()
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
