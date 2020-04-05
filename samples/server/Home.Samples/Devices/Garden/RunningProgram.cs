using Lucky.Db;
using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
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
        private readonly ImmediateProgram _cycle;
        private readonly FileInfo _csvFile;

        private double _startQty = 0;
        private GardenCsvRecord _data;
        // Liters when cycle changes
        private double _partialQty = 0;
        // Time when cycle changes
        private DateTime _partialTime = DateTime.Now;
        private List<ZoneTimeWithQuantity> _results = new List<ZoneTimeWithQuantity>();

        private class MailData
        {
            public string Name;
            public ZoneTimeWithQuantity[] ZoneData;
        }
        private List<MailData> _mailData = new List<MailData>();

        public RunningProgram(ImmediateProgram cycle, ILogger logger, GardenDevice device)
        {
            _device = device;
            _logger = logger;
            _cycle = cycle;
            _logger.Log("Garden", "cycle start", cycle.Name);

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
            _data = new GardenCsvRecord
            {
                Date = now.Date,
                Time = now.TimeOfDay,
                Cycle = _cycle.Name,
                Zones = string.Join(";", _cycle.ZoneTimes.Where(t => t.Minutes > 0).Select(t => ZoneDetailsToString(GardenDevice.ToZoneMask(t.Zones), t.Minutes))),
                State = 1,
            };

            var flowData = await _device.ReadFlow();
            if (flowData != null)
            {
                _startQty = _data.TotalQtyMc = flowData.TotalMc;
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
        public async Task Stop(DateTime now1)
        {
            _logger.Log("Garden", "cycle end", _cycle.Name);
            if (_startQty > 0)
            {
                var flowData1 = await _device.ReadFlow();
                if (flowData1 != null)
                {
                    _data.QtyL = (flowData1.TotalMc - _startQty) * 1000.0;
                    _data.TotalQtyMc = flowData1.TotalMc;
                    _data.FlowLMin = flowData1.FlowLMin;
                }
            }

            _data.State = 0;
            _data.Date = now1.Date;
            _data.Time = now1.TimeOfDay;
            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, _data);
            }

            ScheduleMail(now1, _cycle.Name, _results.Where(t => t != null).ToArray());
        }

        public async Task Step(DateTime now1, GardenSink.TimerState state)
        {
            // Skip leading zeroes to understand which is the current operative zone
            // Can be cycle count + 1
            var currentCycle = state.ZoneRemTimes
                    .Concat(new GardenSink.ImmediateZoneTime[1] { null })
                    .Select((t, i) => Tuple.Create(t, i))
                    .First(t => t.Item1 == null || t.Item1.Time > 0).Item2;

            // Calc CSV line with total quantity
            double currentQtyL = -1.0;
            if (_startQty > 0)
            {
                var flowData1 = await _device.ReadFlow();
                if (flowData1 != null)
                {
                    _data.State = 2;
                    _data.Date = now1.Date;
                    _data.Time = now1.TimeOfDay;
                    currentQtyL = _data.QtyL = (flowData1.TotalMc - _startQty) * 1000.0;
                    _data.TotalQtyMc = flowData1.TotalMc;
                    _data.FlowLMin = flowData1.FlowLMin;
                    _data.Zones = string.Join(";", state.ZoneRemTimes.Select(t => ZoneDetailsToString(t.ZoneMask, t.Time)));
                    lock (_csvFile)
                    {
                        CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, _data);
                    }
                }
            }

            double qtyL = currentQtyL;
            if (qtyL > 0)
            {
                qtyL -= _partialQty;
            }

            lock (_results)
            {
                while (currentCycle > _results.Count && _results.Count < _cycle.ZoneTimes.Length)
                {
                    DateTime now2 = DateTime.Now;
                    var inputData = _cycle.ZoneTimes[_results.Count];
                    // Ok calc results of previous cycle
                    if (inputData.Minutes > 0)
                    {
                        _results.Add(new ZoneTimeWithQuantity
                        {
                            Zones = inputData.Zones,
                            Minutes = (int)Math.Round((now2 - _partialTime).TotalMinutes),
                            QuantityL = (int)Math.Round(qtyL)
                        });
                    }
                    else
                    {
                        // It was not programmed
                        _results.Add(null);
                    }

                    _partialQty = currentQtyL;
                    _partialTime = now2;
                }
            }
        }

        private void ScheduleMail(DateTime now, string name, ZoneTimeWithQuantity[] results)
        {
            _mailData.Add(new MailData
            {
                Name = name,
                ZoneData = results
            });

            // If more programs will follow, don't send the mail now
            var nextCycle = _device.GetNextCycle(now);
            if (nextCycle == null || nextCycle.Item2 > (now + TimeSpan.FromMinutes(5)))
            {
                // Schedule mail
                string body = "Cicli effettuati:" + Environment.NewLine;
                body += string.Join(
                    Environment.NewLine,
                    _mailData.Select(data =>
                        string.Format("{0} \r\n{1}",
                            data.Name,
                            string.Join(Environment.NewLine,
                                data.ZoneData.Select(t =>
                                {
                                    return string.Format("  {0}: {1} minuti ({2} litri)", GetZoneNames(t.Zones), t.Minutes, t.QuantityL);
                                })
                            )
                        )
                    )
                );

                Manager.GetService<INotificationService>().SendMail("Giardino irrigato", body, false);
                _mailData.Clear();
            }
        }

        private string GetZoneNames(int[] index)
        {
            return string.Join(", ", index.Select(i => _device.GetZoneName(i)));
        }
    }
}
