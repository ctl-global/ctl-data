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
            using (ExcelPackage pkg = new ExcelPackage(new FileInfo("Marsh R2_2014_Backer Tags - Copy.xlsx")))
            {
                foreach (var ws in pkg.Workbook.Worksheets)
                {
                    Console.WriteLine("{0}: {1:N0}", ws.Name, new Ctl.Data.Excel.ExcelReader(ws).AsEnumerable().Count());
                }
            }
        }
    }
}
