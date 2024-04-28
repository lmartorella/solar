using System;
using System.IO;
using System.Linq;

namespace Lucky.Db
{
    /// <summary>
    /// Aggregate data in a csv file
    /// </summary>
    public class CsvAggregate<T, Taggr> where T : TimeSample, new() where Taggr : DayTimeSample<T>, new()
    {
        public static Taggr ParseCsv(FileInfo file, DateTime date)
        {
            var data = CsvHelper<T>.ReadCsv(file);
            var ret = new Taggr();
            if (ret.Aggregate(date, data))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }

        private static DateTime? ParseDate(string name, string fileNameFormat)
        {
            DateTime date;
            if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(name), fileNameFormat, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
            else
            {
                return null;
            }
        }

        public static Tuple<FileInfo, DateTime>[] GetFilesInFolder(DirectoryInfo directory, string fileNameFormat, DateTime now)
        {
            return directory
                .GetFiles()
                .Select(file => Tuple.Create(file, ParseDate(file.Name, fileNameFormat)))
                .Where(t => t.Item2.HasValue && t.Item2.Value < now.Date)
                .Select(tuple => Tuple.Create(tuple.Item1, tuple.Item2.Value))
                .OrderBy(t => t.Item2)
                .ToArray();
        }
    }
}
