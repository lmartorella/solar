using Lucky.Db;
using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    /// <summary>
    /// Record activity about a running program
    /// </summary>
    class RunningProgram
    {
        private GardenDevice _device;
        private readonly ILogger _logger;
        private readonly ZoneTime _zoneTimes;
        private readonly FileInfo _csvFile;
        private string _name;

        private double _startQtyMc = 0;
        private GardenCsvRecord _data;
        private double _currentQtyMc = 0;
        private DateTime _startTime;
        private DateTime _currentTime;

        public RunningProgram(ZoneTime zoneTimes, ILogger logger, GardenDevice device, string name)
        {
            _device = device;
            _logger = logger;
            _name = name;
            _zoneTimes = zoneTimes;
            _logger.Log("Garden", "cycle start", name);

            // Prepare CSV file
            var dbFolder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/GARDEN"));
            _csvFile = new FileInfo(Path.Combine(dbFolder.FullName, "garden.csv"));
            if (!_csvFile.Exists)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvHeader(_csvFile);
            }
        }

        public async Task Start(DateTime now)
        {
            _currentTime = _startTime = now;
            
            _data = new GardenCsvRecord
            {
                Date = now.Date,
                Time = now.TimeOfDay,
                Cycle = _name,
                Zones = ZoneDetailsToString(GardenDevice.ToZoneMask(_zoneTimes.Zones), _zoneTimes.Minutes),
                State = 1,
            };

            var flowData = await _device.ReadFlow();
            if (flowData != null)
            {
                _startQtyMc = _data.TotalQtyMc = flowData.TotalMc;
            }

            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, _data);
            }
        }

        private string ZoneDetailsToString(byte zoneMask, int minutes)
        {
            return string.Format("0x{0:X2}={1}", zoneMask, minutes);
        }

        // Return the action to log the stop program
        public async Task Stop(DateTime now)
        {
            _logger.Log("Garden", "cycle end", _name);
            if (_startQtyMc > 0)
            {
                var flowData1 = await _device.ReadFlow();
                if (flowData1 != null)
                {
                    _data.QtyL = (flowData1.TotalMc - _startQtyMc) * 1000.0;
                    _data.TotalQtyMc = flowData1.TotalMc;
                    _data.FlowLMin = flowData1.FlowLMin;
                }
            }

            _data.State = 0;
            _data.Date = now.Date;
            _data.Time = now.TimeOfDay;
            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, _data);
            }

            IsStopped = true;
            _device.ScheduleMail(now, _name, (int)((_currentQtyMc - _startQtyMc) * 1000.0), (int)(_currentTime - _startTime).TotalMinutes);
        }

        public bool IsStopped;

        public async Task Step(DateTime now1, GardenSink.TimerState state)
        {
            // Skip leading zeroes to understand which is the current operative zone
            // Can be cycle count + 1
            var currentCycle = state.ZoneRemTimes
                    .Concat(new GardenSink.ImmediateZoneTime[1] { null })
                    .Select((t, i) => Tuple.Create(t, i))
                    .First(t => t.Item1 == null || t.Item1.Time > 0).Item2;

            // Calc CSV line with total quantity
            if (_startQtyMc > 0)
            {
                var flowData1 = await _device.ReadFlow();
                if (flowData1 != null)
                {
                    _data.State = 2;
                    _data.Date = now1.Date;
                    _data.Time = now1.TimeOfDay;
                    _data.QtyL = (flowData1.TotalMc - _startQtyMc) * 1000.0;
                    _data.TotalQtyMc = flowData1.TotalMc;
                    _data.FlowLMin = flowData1.FlowLMin;
                    _data.Zones = string.Join(";", state.ZoneRemTimes.Select(t => ZoneDetailsToString(t.ZoneMask, t.Time)));
                    lock (_csvFile)
                    {
                        CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, _data);
                    }
                    _currentQtyMc = flowData1.TotalMc;
                }
            }

            _currentTime = now1;
        }

        public NextCycle ToNextCycle(Configuration configuration)
        {
            return new NextCycle(_zoneTimes, configuration, true);
        }
    }
}
