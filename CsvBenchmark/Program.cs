using LINQtoCSV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ctl.Data;
using System.Security;

namespace CsvBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes("worldcitiespop.txt");

            Benchmark(data);
            //Compare(data);
        }

        static void Compare(byte[] data)
        {
            string[][] ctlRecords = new Ctl.Data.CsvReader(new StreamReader(new MemoryStream(data, false))).AsEnumerable().Select(x => x.Select(y => y.Value).ToArray()).ToArray();

            CompareRaw("CsvHelper", ctlRecords, CsvHelperEnum(data));
            CompareRaw("LinqToCSV", ctlRecords, LinqToCsvEnum(data));
            CompareRaw("TextFieldParser", ctlRecords, TextFieldParserEnum(data));
        }

        static void Benchmark(byte[] data)
        {
            Benchmark("Univocity (raw)", () =>
            {
                int count = 0;

                var settings = new com.univocity.parsers.csv.CsvParserSettings();
                var parser = new com.univocity.parsers.csv.CsvParser(settings);

                parser.beginParsing(new java.io.InputStreamReader(new java.io.ByteArrayInputStream(data)));
                while (parser.parseNext() != null) ++count;
                parser.stopParsing();

                return count;
            });

            Benchmark("Ctl.Data (raw)", () =>
            {
                int count = 0;

                var csv = new Ctl.Data.CsvReader(new StreamReader(new MemoryStream(data, false)));
                while (csv.Read()) ++count;

                return count;
            });

            Benchmark("Ctl.Data (POCO)", () =>
            {
                int count = 0;

                var csv = new Ctl.Data.CsvReader<Record>(new StreamReader(new MemoryStream(data, false)));
                while (csv.Read()) ++count;

                return count;
            });

            Benchmark("Ctl.Data (hand-POCO)", () =>
            {
                int count = 0;

                var csv = new Ctl.Data.CsvReader(new StreamReader(new MemoryStream(data, false)));

                csv.Read(); // skip header.

                while (csv.Read())
                {
                    var row = csv.CurrentRow;

                    string popStr = row[4].Value;
                    int? popInt = !string.IsNullOrEmpty(popStr) ? int.Parse(popStr, CultureInfo.InvariantCulture) : (int?)null;

                    Record r = new Record
                    {
                        Country = row[0].Value,
                        City = row[1].Value,
                        AccentCity = row[2].Value,
                        Region = row[3].Value,
                        Population = popInt,
                        Latitude = double.Parse(row[5].Value, CultureInfo.InvariantCulture),
                        Longitude = double.Parse(row[6].Value, CultureInfo.InvariantCulture)
                    };

                    ++count;
                }

                return count;
            });

            Benchmark("Ctl.Data (raw async)", async () =>
            {
                int count = 0;

                var csv = new Ctl.Data.CsvReader(new StreamReader(new MemoryStream(data, false)));
                while (await csv.ReadAsync(CancellationToken.None).ConfigureAwait(false)) ++count;

                return count;
            }).Wait();

            Benchmark("Ctl.Data (POCO async)", async () =>
            {
                int count = 0;

                var csv = new Ctl.Data.CsvReader<Record>(new StreamReader(new MemoryStream(data, false)));
                while (await csv.ReadAsync(CancellationToken.None).ConfigureAwait(false)) ++count;

                return count;
            }).Wait();

            Benchmark("TextFieldParser", () =>
            {
                int count = 0;

                var p = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StreamReader(new MemoryStream(data, false)));

                p.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                p.SetDelimiters(",");

                while (!p.EndOfData)
                {
                    p.ReadFields();
                    ++count;
                }

                return count;
            });

            Benchmark("CsvHelper (raw)", () =>
            {
                int count = 0;

                CsvHelper.CsvParser p = new CsvHelper.CsvParser(new StreamReader(new MemoryStream(data, false)));
                while (p.Read() != null) ++count;

                return count;
            });

            Benchmark("CsvHelper (POCO)", () =>
            {
                CsvHelper.CsvReader r = new CsvHelper.CsvReader(new StreamReader(new MemoryStream(data, false)));
                return r.GetRecords<Record>().Count();
            });

            Benchmark("LinqToCSV (raw)", () =>
            {
                CsvContext ctx = new CsvContext();
                return ctx.Read<MyDataRow>(new StreamReader(new MemoryStream(data, false))).Count();
            });

            Benchmark("LinqToCSV (POCO)", () =>
            {
                CsvContext ctx = new CsvContext();
                return ctx.Read<Record>(new StreamReader(new MemoryStream(data, false))).Count();
            });
        }

        static IEnumerable<string[]> CsvHelperEnum(byte[] data)
        {
            CsvHelper.CsvParser p = new CsvHelper.CsvParser(new StreamReader(new MemoryStream(data, false)));

            string[] r;

            while ((r = p.Read()) != null)
            {
                yield return r;
            }
        }

        static IEnumerable<string[]> LinqToCsvEnum(byte[] data)
        {
            CsvContext ctx = new CsvContext();
            CsvFileDescription desc = new CsvFileDescription { FirstLineHasColumnNames = false };

            foreach (var r in ctx.Read<MyDataRow>(new StreamReader(new MemoryStream(data, false)), desc))
            {
                yield return r.Select(x => x.Value).ToArray();
            }
        }

        static IEnumerable<string[]> TextFieldParserEnum(byte[] data)
        {
            var p = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StreamReader(new MemoryStream(data, false)));

            p.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            p.TrimWhiteSpace = false;
            p.SetDelimiters(",");

            while (!p.EndOfData)
            {
                yield return p.ReadFields();
            }
        }

        static void CompareRaw(string name, IEnumerable<string[]> left, IEnumerable<string[]> right)
        {
            int rowIdx = 0;

            using (var eLeft = left.GetEnumerator())
            using (var eRight = right.GetEnumerator())
            {
                while(true)
                {
                    ++rowIdx;

                    bool nextLeft = eLeft.MoveNext();
                    bool nextRight = eRight.MoveNext();

                    if (nextLeft != nextRight)
                    {
                        throw new Exception(string.Format("Row count mismatch on {0} with {1}.", rowIdx, name));
                    }

                    if (!nextLeft) break;

                    if (!eLeft.Current.Select(x=>(x ?? string.Empty).Trim()).SequenceEqual(eRight.Current.Select(x=>(x ?? string.Empty).Trim())))
                    {
                        throw new Exception(string.Format("Row value mismatch on {0} with {1}.", rowIdx, name));
                    }
                }
            }
        }

        class MyDataRow : List<DataRowItem>, IDataRow
        {
        }

        class Record
        {
            public string Country { get; set; }
            public string City { get; set; }
            public string AccentCity { get; set; }
            public string Region { get; set; }
            public int? Population { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        static void Benchmark(string name, Func<int> func)
        {
            Console.Write(name);

            // warm-up, get a rough idea of how many times it can run in one second.

            int runCount = 0, itemCount;
            Stopwatch sw = Stopwatch.StartNew();

            do
            {
                itemCount = func();
                ++runCount;
            }
            while (sw.ElapsedTicks < Stopwatch.Frequency);

            Console.Write("..." + runCount.ToString("N0") + " runs");

            // loop runcount until we hit 5 runs without any time improvement.

            long bestTicks = long.MaxValue;
            int goodLoops = 0;

            while (true)
            {
                sw.Restart();

                for (int i = 0; i < runCount; ++i)
                {
                    func();
                }

                long curTicks = sw.ElapsedTicks;

                if (curTicks < bestTicks)
                {
                    bestTicks = curTicks;
                    goodLoops = 0;
                    Console.Write("+");
                }
                else if (++goodLoops == 5)
                {
                    break;
                }
                else
                {
                    Console.Write(".");
                }
            }

            double itemsPerSecond = runCount * itemCount * Stopwatch.Frequency / (double)bestTicks;

            Console.WriteLine(" {0:F2} records/sec.", itemsPerSecond);
        }

        static async Task Benchmark(string name, Func<Task<int>> func)
        {
            Console.Write(name);

            // warm-up, get a rough idea of how many times it can run in one second.

            int runCount = 0, itemCount;
            Stopwatch sw = Stopwatch.StartNew();

            do
            {
                itemCount = await func().ConfigureAwait(false);
                ++runCount;
            }
            while (sw.ElapsedTicks < Stopwatch.Frequency);

            Console.Write("..." + runCount.ToString("N0") + " runs");

            // loop runcount until we hit 5 runs without any time improvement.

            long bestTicks = long.MaxValue;
            int goodLoops = 0;

            while (true)
            {
                sw.Restart();

                for (int i = 0; i < runCount; ++i)
                {
                    await func().ConfigureAwait(false);
                }

                long curTicks = sw.ElapsedTicks;

                if (curTicks < bestTicks)
                {
                    bestTicks = curTicks;
                    goodLoops = 0;
                    Console.Write("+");
                }
                else if (++goodLoops == 5)
                {
                    break;
                }
                else
                {
                    Console.Write(".");
                }
            }

            double itemsPerSecond = runCount * itemCount * Stopwatch.Frequency / (double)bestTicks;

            Console.WriteLine(" {0:F2} records/sec.", itemsPerSecond);
        }
    }
}
