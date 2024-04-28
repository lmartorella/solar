using Lucky.Db;
using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Db
{
    /// <summary>
    /// A day-by-day time series that stores data in CSV files (one for each day).
    /// Supports DST translation of timestamp (stores everything in non-DST time) for easy comparison (e.g. solar panel outputs).
    /// Stores an additional aggregation CSV (_aggr.csv) for quick startup after restart.
    /// </summary>
    public class FsTimeSeries<T, Taggr> : ITimeSeries<T, Taggr> where T : TimeSample, new() where Taggr : DayTimeSample<T>, new()
    {
        internal const string FILENAME_FORMAT = "yyyy-MM-dd";

        private DirectoryInfo _folder;
        private FileInfo _dayFile;
        private FileInfo _aggrFile;

        private PeriodData _currentPeriod;
        private ILogger _logger;
        private readonly object _lockDb = new object();

        private class PeriodData
        {
            private List<T> _data = new List<T>();
            private readonly TimeSpan _daylightDelta = TimeSpan.Zero;

            public DateTime Begin { get; }

            public PeriodData(DateTime begin, bool useSummerTime)
            {
                Begin = begin;
                Add(new T() { TimeStamp = begin }, true);

                // Check if DST. However DST is usually starting at 3:00 AM, so use midday time
                if ((begin.Date + TimeSpan.FromHours(12)).IsDaylightSavingTime() && useSummerTime)
                {
                    // Calc summer time offset
                    var rule = TimeZoneInfo.Local.GetAdjustmentRules().FirstOrDefault(r =>
                    {
                        return (begin > r.DateStart && begin < r.DateEnd);
                    });
                    if (rule != null)
                    {
                        _daylightDelta = rule.DaylightDelta;
                    }
                }
            }

            public void ParseCsv(FileInfo file)
            {
                foreach (var sample in CsvHelper<T>.ReadCsv(file))
                {
                    Add(sample, false);
                }
            }

            internal T GetLastSample()
            {
                lock (_data)
                {
                    // Skip the first, added in the ctor to maintain the begin timestamp
                    return _data.Skip(1).LastOrDefault();
                }
            }

            public void Add(T sample, bool convert)
            {
                lock (_data)
                {
                    // Convert TS to non-daylight saving time
                    if (convert)
                    {
                        sample.TimeStamp = ToInvariantTime(sample.TimeStamp);
                    }
                    sample.DaylightDelta = _daylightDelta;
                    _data.Add(sample);
                }
            }

            /// <summary>
            /// Convert a DST/non-DST time to invariant non-DST time
            /// </summary>
            private DateTime ToInvariantTime(DateTime ts)
            {
                return ts - _daylightDelta;
            }

            public Taggr GetAggregatedData()
            {
                var ret = new Taggr();
                ret.DaylightDelta = _daylightDelta;
                lock (_data)
                {
                    if (ret.Aggregate(Begin.Date, _data))
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

        /// <summary>
        /// Build a time series manager in a folder.
        /// </summary>
        /// <param name="folderPath">The relative path to the {etc}/Db/ path.</param>
        public FsTimeSeries(string folderPath)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("Db/" + folderPath);
            _folder =  new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/" + folderPath));
            _aggrFile = new FileInfo(Path.Combine(_folder.FullName, "_aggr.csv"));

            _logger.Log("Started");
        }

        private static string CalcDayCvsName(DateTime now)
        {
            return now.ToString(FILENAME_FORMAT) + ".csv";
        }

        /// <summary>
        /// Called at startup to sync aggregate data
        /// </summary>
        public async Task Init(DateTime start)
        {
            await SetupCurrentDb(start, true);
            AggregateData(_logger, _folder, start);
        }

        /// <summary>
        /// Called at the end of a day to open a new csv file
        /// </summary>
        public async Task Rotate(DateTime start)
        {
            await SetupCurrentDb(start, false);
            _logger.Log("Rotated", "date", start.Date);
        }

        private async Task SetupCurrentDb(DateTime start, bool init)
        { 
            var fileName = CalcDayCvsName(start);
            Task copy = null;
            lock (_lockDb)
            {
                var oldFileName = _dayFile;
                var oldPeriod = _currentPeriod;

                // Change filename, so open a new file
                _dayFile = new FileInfo(Path.Combine(_folder.FullName, fileName));
                _currentPeriod = new PeriodData(start, true);

                // Write CSV header
                if (!_dayFile.Exists)
                {
                    CsvHelper<T>.WriteCsvHeader(_dayFile);
                }
                else
                {
                    // Read the CSV and populate the current Period Data
                    _currentPeriod.ParseCsv(_dayFile);
                }

                // Now the old file is free to be copied in backup
                if (!init)
                {
                    copy = CopyToBackup(oldFileName);

                    // Generate aggregated data and log it in a separate DB
                    var aggrData = oldPeriod.GetAggregatedData();
                    if (aggrData != null)
                    {
                        aggrData.Date = oldPeriod.Begin;

                        if (!_aggrFile.Exists)
                        {
                            CsvHelper<Taggr>.WriteCsvHeader(_aggrFile);
                        }
                        CsvHelper<Taggr>.WriteCsvLine(_aggrFile, aggrData);
                    }
                }
            }

            if (copy != null)
            {
                await copy;
            }
        }

        /// <summary>
        /// Return aggregated data for the whole DB.
        /// </summary>
        public Taggr GetAggregatedData()
        {
            return _currentPeriod.GetAggregatedData();
        }

        /// <summary>
        /// Register new sample
        /// </summary>
        public void AddNewSample(T sample)
        {
            lock (_lockDb)
            {
                // Add it to the aggregator frist, so daylight time will be updated
                _currentPeriod.Add(sample, true);

                // In addition, write immediately on the CSV file.
                CsvHelper<T>.WriteCsvLine(_dayFile, sample);
            }
        }

        public T GetLastSample()
        {
            lock (_lockDb)
            {
                return _currentPeriod.GetLastSample();
            }
        }

        private async Task CopyToBackup(FileInfo oldFileName)
        {
            int retry = 0;
            oldFileName.Refresh();
            while (!oldFileName.Exists)
            {
                await Task.Delay(2000);
                if (retry++ == 5)
                {
                    Manager.GetService<INotificationService>().EnqueueStatusUpdate("Errori DB", "Cannot find file to backup after 5 retries");
                    _logger.Log("Cannot find file to backup after 5 retries", "src", oldFileName.FullName);
                    return;
                }
                oldFileName.Refresh();
            }

            var targetFile = GetBackupPath(oldFileName);
            try
            {
                _logger.Log("Making backup", "src", oldFileName.FullName, "dst", targetFile.FullName);
                oldFileName.CopyTo(targetFile.FullName);
            }
            catch (Exception exc)
            {
                _logger.Exception(exc);
                Manager.GetService<INotificationService>().EnqueueStatusUpdate("File locked", "Cannot backup the db file: " + oldFileName.FullName + Environment.NewLine + "EXC: " + exc.Message);
            }
        }

        private FileInfo GetBackupPath(FileInfo srcFileName)
        {
            DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(srcFileName.Directory.FullName, "backup"));
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            string baseName = srcFileName.Name;
            string targetFileName = baseName;
            var targetFile = new FileInfo(Path.Combine(targetDir.FullName, targetFileName));
            int c = 1;
            while (targetFile.Exists)
            {
                targetFileName = baseName + string.Format(" ({0})", c++);
                targetFile = new FileInfo(Path.Combine(targetDir.FullName, targetFileName));
            }
            return targetFile;
        }

        /// <summary>
        /// Aggregate whole DB content at startup
        /// </summary>
        public static void AggregateData(ILogger logger, DirectoryInfo directory, DateTime now)
        {
            FileInfo aggrFile = new FileInfo(Path.Combine(directory.FullName, "_aggr.csv"));
            if (aggrFile.Exists)
            {
                logger.Log("Aggregated csv already exists, using it");
                return;
            }

            CsvHelper<Taggr>.WriteCsvHeader(aggrFile);

            // Skip the current/future dates
            var files = CsvAggregate<T, Taggr>.GetFilesInFolder(directory, FILENAME_FORMAT, now);
            logger.Log("Parsing csv files", "n", files.Length);
            foreach (var tuple in files)
            {
                // Parse CSV
                var aggrData = CsvAggregate<T, Taggr>.ParseCsv(tuple.Item1, tuple.Item2);
                if (aggrData != null)
                {
                    CsvHelper<Taggr>.WriteCsvLine(aggrFile, aggrData);
                }
            }
            logger.Log("Parsing csv files done");
        }
    }
}
