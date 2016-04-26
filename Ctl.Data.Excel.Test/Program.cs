using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
                ReadFormatted = true
            };

            using (ExcelPackage pkg = new ExcelPackage(new FileInfo("Book1.xlsx")))
            {
                foreach (var ws in pkg.Workbook.Worksheets)
                {
                    foreach (var rv in new Ctl.Data.Excel.ExcelReader(ws, opts).AsEnumerable())
                    {
                        Console.WriteLine(rv[0].Value);
                    }
                }
            }
        }
    }
}
