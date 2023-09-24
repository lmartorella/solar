using System;
using System.IO;

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
    }
}
