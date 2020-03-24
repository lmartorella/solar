using Lucky.Db;
using Lucky.Home.Model;
using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using static Lucky.Home.Devices.GardenDevice;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class GardenWebResponse : WebResponse
    {
        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Result/error
        /// </summary>
        [DataMember(Name = "error")]
        public string Error { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        [DataMember(Name = "config")]
        public Configuration Configuration { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "online")]
        public bool Online { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "flowData")]
        public FlowData FlowData { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "nextCycles")]
        public NextCycle[] NextCycles { get; set; }
    }

    /// <summary>
    /// JSON for configuration serialization
    /// </summary>
    [DataContract]
    public class Configuration
    {
        [DataMember(Name = "program")]
        public TimeProgram<GardenCycle>.ProgramData Program { get; set; }

        [DataMember(Name = "zones")]
        public string[] ZoneNames { get; set; }
    }

    [DataContract]
    public class NextCycle
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "scheduledTime")]
        public string ScheduledTimeStr { get { return ScheduledTime.ToIso(); } set { ScheduledTime = value.FromIso(); } }
        [IgnoreDataMember]
        public DateTime? ScheduledTime { get; set; }
    }

    class GardenCsvRecord
    {
        [Csv("yyyy-MM-dd")]
        public DateTime Date;

        /// <summary>
        /// Time of day of the first sample with power > 0
        /// </summary>
        [Csv("hh\\:mm\\:ss")]
        public TimeSpan Time;

        [Csv]
        public string Cycle;

        [Csv]
        public string Zones;

        /// <summary>
        /// 0: stoped (final). 1 = just started. 2 = flowing
        /// </summary>
        [Csv]
        public int State;

        /// <summary>
        /// In liter/seconds
        /// </summary>
        [Csv("0.0")]
        public double FlowLMin;

        /// <summary>
        /// Liter used for cycle
        /// </summary>
        [Csv("0.0")]
        public double QtyL;

        /// <summary>
        /// Total MC
        /// </summary>
        [Csv("0.000")]
        public double TotalQtyMc;
    }

    /// <summary>
    /// Control and diagnose a custom garden programmer
    /// </summary>
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    [Requires(typeof(FlowSink))]
    public class GardenDevice : DeviceBase
    {
        private static int POLL_PERIOD = 3000;
        private FileInfo _cfgFile;
        private Timer _debounceTimer;
        private object _timeProgramLock = new object();
        private readonly TimeProgram<GardenCycle> _timeProgram;
        private readonly Queue<ImmediateProgram> _cycleQueue = new Queue<ImmediateProgram>();
        private readonly FileInfo _csvFile;
        private string[] _zoneNames = new string[0];
        private readonly double _counterFq;
        private List<MailData> _mailData = new List<MailData>();

        private class MailData
        {
            public string Name;
            public ZoneTimeWithQuantity[] ZoneData;
        }

        [DataContract]
        public class GardenCycle : TimeProgram<GardenCycle>.Cycle
        {
            /// <summary>
            /// 1 up to 4 "cycles" zone program. Extended: multiple concurrent zone at the same time
            /// </summary>
            [DataMember(Name = "zoneTimes")]
            public ZoneTime[] ZoneTimes;
        }

        [DataContract]
        public class ZoneTime
        {
            [DataMember(Name = "minutes")]
            public int Minutes;

            [DataMember(Name = "zones")]
            public int[] Zones;
        }

        private class ZoneTimeWithQuantity : ZoneTime
        {
            public int QuantityL;
        }

        private class ImmediateProgram
        {
            public ZoneTime[] ZoneTimes;
            public string Name;

            public bool IsEmpty
            {
                get
                {
                    return ZoneTimes == null || ZoneTimes.All(z => z.Minutes <= 0);
                }
            }
        }

        public GardenDevice(double counterFq = 5.5)
        {
            _counterFq = counterFq;
            _timeProgram = new TimeProgram<GardenCycle>(Logger);

            var cfgColder = Manager.GetService<PersistenceService>().GetAppFolderPath("server");
            _cfgFile = new FileInfo(Path.Combine(cfgColder, "gardenCfg.json"));
            if (!_cfgFile.Exists)
            {
                using (var stream = _cfgFile.Open(FileMode.Create))
                {
                    // No data. Write the current settings
                    new DataContractJsonSerializer(typeof(Configuration)).WriteObject(stream, new Configuration { Program = TimeProgram<GardenCycle>.DefaultProgram });
                }
            }

            _timeProgram.CycleTriggered += HandleProgramCycle;
            ReadConfig();

            // Prepare CSV file
            var dbFolder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/GARDEN"));
            _csvFile = new FileInfo(Path.Combine(dbFolder.FullName, "garden.csv"));
            if (!_csvFile.Exists)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvHeader(_csvFile);
            }

            // Subscribe changes
            var cfgFileObserver = new FileSystemWatcher(cfgColder, "gardenCfg.json");
            cfgFileObserver.Changed += (o, e) => Debounce(() => ReadConfig());
            cfgFileObserver.NotifyFilter = NotifyFilters.LastWrite;
            cfgFileObserver.EnableRaisingEvents = true;

            // To receive commands from UI
            Manager.GetService<PipeServer>().Message += (o, e) =>
            {
                switch (e.Request.Command)
                {
                    case "garden.getStatus":
                        e.Response = Task.Run(async () =>
                        {
                            FlowData flowData = await ReadFlow();
                            
                            // Tactical
                            if (flowData != null)
                            {
                                await GetFirstOnlineSink<GardenSink>()?.UpdateFlowData((int)flowData.FlowLMin);
                            }

                            var nextCycles = _cycleQueue.Select(q => Tuple.Create(q.Name, (DateTime?)null))
                                            .Concat(_timeProgram?.GetNextCycles(DateTime.Now).Select(c => Tuple.Create(c.Item1.Name, (DateTime?)c.Item2)))
                                            .Take(4);

                            return (WebResponse) new GardenWebResponse {
                                Online = GetFirstOnlineSink<GardenSink>() != null,
                                Configuration = new Configuration { Program = _timeProgram.Program, ZoneNames = _zoneNames },
                                FlowData = flowData,
                                NextCycles = nextCycles.Select(t => new NextCycle { Name = t.Item1, ScheduledTime = t.Item2 }).ToArray()
                            };
                        });
                        break;
                    case "garden.setImmediate":
                        Logger.Log("setImmediate", "msg", string.Join(",", e.Request.ImmediateZones.Select(z => z.ToString())));
                        e.Response = Task.FromResult((WebResponse) new GardenWebResponse
                        {
                            Error = ScheduleCycle(new ImmediateProgram
                            {
                                ZoneTimes = e.Request.ImmediateZones.Select(prg => new ZoneTime
                                {
                                    Minutes = prg.Time,
                                    Zones = prg.Zones
                                }).ToArray(),
                                Name = "Immediato"
                            })
                        });
                        break;
                    case "garden.stop":
                        bool stopped = false;
                        foreach (var sink in Sinks.OfType<GardenSink>())
                        {
                            sink.ResetNode();
                            stopped = true;
                        }
                        string error = null;
                        if (!stopped)
                        {
                            error = "Cannot stop, no sink";
                        }
                        e.Response = Task.FromResult((WebResponse) new GardenWebResponse { Error = error });
                        break;
                }
            };

            StartLoop();
        }

        private async Task<FlowData> ReadFlow()
        {
            var flowSink = GetFirstOnlineSink<FlowSink>();
            if (flowSink != null)
            {
                try
                {
                    return await flowSink.ReadData(_counterFq, POLL_PERIOD);
                }
                catch (Exception exc)
                {
                    Logger.Exception(exc);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Persistent loop
        /// </summary>
        /// <returns></returns>
        private async Task StartLoop()
        {
            StepActions lastStepActions = null;
            bool inProgress = false;
            int errors = 0;

            while (!IsDisposed)
            {
                await Task.Delay(POLL_PERIOD);

                // Program in progress?
                bool cycleIsWaiting = false;
                // Check for new program to run
                lock (_cycleQueue)
                {
                    cycleIsWaiting = _cycleQueue.Count > 0;
                }

                // Do I need to contact the garden sink?
                if (inProgress || cycleIsWaiting)
                {
                    var gardenSink = GetFirstOnlineSink<GardenSink>();
                    if (gardenSink != null)
                    {
                        // Wait for a free garden device
                        var state = await gardenSink.Read(false, POLL_PERIOD);
                        if (state == null)
                        {
                            // Lost connection with garden programmer!
                            if (errors++ < 5)
                            {
                                Logger.Log("Cannot contact garden (2)", "cycleIsWaiting", cycleIsWaiting, "inProgress", inProgress);
                                continue;
                            }
                        }

                        errors = 0;
                        DateTime now = DateTime.Now;
                        if (state.IsAvailable)
                        {
                            // Finished?
                            if (lastStepActions != null)
                            {
                                lastStepActions.StopAction(now);
                                lastStepActions = null;
                                inProgress = false;
                            }

                            // New program to load?
                            if (cycleIsWaiting)
                            {
                                ImmediateProgram cycle;
                                GardenSink.ImmediateZoneTime[] zoneTimes = null;
                                lock (_cycleQueue)
                                {
                                    cycle = _cycleQueue.Dequeue();
                                    zoneTimes = cycle.ZoneTimes.Select(t => new GardenSink.ImmediateZoneTime { Time = (byte)t.Minutes, ZoneMask = ToZoneMask(t.Zones) }).ToArray();
                                }
                                await gardenSink.WriteProgram(zoneTimes);
                                inProgress = true;
                                lastStepActions = await LogStartProgram(now, cycle);
                            }
                        }
                        else
                        {
                            if (inProgress)
                            {
                                // The program is running? Log flow
                                await lastStepActions.StepAction(now, state);
                            }
                        }
                    }
                    else
                    {
                        // Lost connection with garden programmer!
                        if (errors++ < 5)
                        {
                            Logger.Log("Cannot contact garden", "cycleIsWaiting", cycleIsWaiting, "inProgress", inProgress);
                        }
                    }
                }
            }
        }

        protected override Task OnTerminate()
        {
            lock (_timeProgramLock)
            {
                _timeProgram.Dispose();
            }
            return base.OnTerminate();
        }

        /// <summary>
        /// Used to read config when the FS notifies changes
        /// </summary>
        private void Debounce(Action handler)
        {
            // Event comes two time (the first one with an empty file)
            if (_debounceTimer == null)
            {
                _debounceTimer = new Timer(o => 
                {
                    _debounceTimer = null;
                    handler();
                }, null, 1000, Timeout.Infinite);
            }
        }

        private void ReadConfig()
        {
            lock (_timeProgramLock)
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Configuration));
                Configuration configuration;
                try
                {
                    using (var stream = File.Open(_cfgFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        configuration = serializer.ReadObject(stream) as Configuration;
                    }
                }
                catch (Exception exc)
                {
                    Logger.Log("Cannot read garden configuration", "Exc", exc.Message);
                    return;
                }

                // Apply configuration
                Logger.Log("New configuration acquired", "cycles#", configuration?.Program?.Cycles?.Length);

                try
                {
                    _timeProgram.SetProgram(configuration.Program);
                    _zoneNames = configuration.ZoneNames ?? new string[0];
                }
                catch (ArgumentException exc)
                {
                    // Error applying configuration
                    Logger.Log("CONFIGURATION ERROR", "exc", exc.Message);
                }
            }
        }

        private void HandleProgramCycle(object sender, TimeProgram<GardenCycle>.CycleTriggeredEventArgs e)
        {
            Logger.Log("ScheduleProgram", "name", e.Cycle.Name);
            ScheduleCycle(new ImmediateProgram { ZoneTimes = e.Cycle.ZoneTimes, Name = e.Cycle.Name } );
        }

        private string ScheduleCycle(ImmediateProgram program)
        {
            if (!program.IsEmpty)
            {
                lock (_cycleQueue)
                {
                    _cycleQueue.Enqueue(program);
                }
                return null;
            }
            else
            {
                return "Empty program";
            }
        }

        private class StepActions
        {
            public Action<DateTime> StopAction;
            public Func<DateTime, GardenSink.TimerState, Task> StepAction;
        }

        private async Task<StepActions> LogStartProgram(DateTime now, ImmediateProgram cycle)
        {
            Logger.Log("Garden", "cycle start", cycle.Name);

            Func<byte, int, string> ZoneDetailsToString = (zoneMask, minutes) =>
            {
                return string.Format("0x{0:X2}={1}", zoneMask, minutes);
            };

            GardenCsvRecord data = new GardenCsvRecord
            {
                Date = now.Date,
                Time = now.TimeOfDay,
                Cycle = cycle.Name,
                Zones = string.Join(";", cycle.ZoneTimes.Where(t => t.Minutes > 0).Select(t => ZoneDetailsToString(ToZoneMask(t.Zones), t.Minutes))),
                State = 1,
            };

            double startQty = 0;

            var flowData = await ReadFlow();
            if (flowData != null)
            {
                startQty = data.TotalQtyMc = flowData.TotalMc;
            }

            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
            }

            List<ZoneTimeWithQuantity> results = new List<ZoneTimeWithQuantity>();
            // Liters when cycle changes
            double partialQty = 0;
            // Time when cycle changes
            DateTime partialTime = DateTime.Now;

            // Return the action to log the stop program
            Action<DateTime> stopAction = async now1 =>
            {
                Logger.Log("Garden", "cycle end", cycle.Name);
                if (startQty > 0)
                {
                    var flowData1 = await ReadFlow();
                    if (flowData1 != null)
                    {
                        data.QtyL = (flowData1.TotalMc - startQty) * 1000.0;
                        data.TotalQtyMc = flowData1.TotalMc;
                        data.FlowLMin = flowData1.FlowLMin;
                    }
                }

                data.State = 0;
                data.Date = now1.Date;
                data.Time = now1.TimeOfDay;
                lock (_csvFile)
                {
                    CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
                }

                ScheduleMail(now1, cycle.Name, results.Where(t => t != null).ToArray());
            };

            // Log a flow info
            Func<DateTime, GardenSink.TimerState, Task> stepAction = async (now1, state) =>
            {
                // Skip leading zeroes to understand which is the current operative zone
                // Can be cycle count + 1
                var currentCycle = state.ZoneRemTimes
                        .Concat(new GardenSink.ImmediateZoneTime[1] { null })
                        .Select((t, i) => Tuple.Create(t, i))
                        .First(t => t.Item1 == null || t.Item1.Time > 0).Item2;

                // Calc CSV line with total quantity
                double currentQtyL = -1.0;
                if (startQty > 0)
                {
                    var flowData1 = await ReadFlow();
                    if (flowData1 != null)
                    {
                        data.State = 2;
                        data.Date = now1.Date;
                        data.Time = now1.TimeOfDay;
                        currentQtyL = data.QtyL = (flowData1.TotalMc - startQty) * 1000.0;
                        data.TotalQtyMc = flowData1.TotalMc;
                        data.FlowLMin = flowData1.FlowLMin;
                        data.Zones = string.Join(";", state.ZoneRemTimes.Select(t => ZoneDetailsToString(t.ZoneMask, t.Time)));
                        lock (_csvFile)
                        {
                            CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
                        }
                    }
                }

                double qtyL = currentQtyL;
                if (qtyL > 0)
                {
                    qtyL -= partialQty;
                }

                lock (results)
                {
                    while (currentCycle > results.Count && results.Count < cycle.ZoneTimes.Length)
                    {
                        DateTime now2 = DateTime.Now;
                        var inputData = cycle.ZoneTimes[results.Count];
                        // Ok calc results of previous cycle
                        if (inputData.Minutes > 0)
                        {
                            results.Add(new ZoneTimeWithQuantity
                            {
                                Zones = inputData.Zones,
                                Minutes = (int)Math.Round((now2 - partialTime).TotalMinutes),
                                QuantityL = (int)Math.Round(qtyL)
                            });
                        }
                        else
                        {
                            // It was not programmed
                            results.Add(null);
                        }

                        partialQty = currentQtyL;
                        partialTime = now2;
                    }
                }
            };

            return new StepActions { StopAction = stopAction, StepAction = stepAction };
        }

        private static byte ToZoneMask(int[] zones)
        {
            byte ret = 0;
            foreach (int zone in zones)
            {
                ret = (byte)(ret | (1 << zone));
            }
            return ret;
        }

        private void ScheduleMail(DateTime now, string name, ZoneTimeWithQuantity[] results)
        {
            _mailData.Add(new MailData
            {
                Name = name,
                ZoneData = results
            });

            bool sendNow = true;
            // If more programs will follow, don't send the mail now
            lock (_timeProgramLock)
            {
                var nextCycle = _timeProgram.GetNextCycles(now).FirstOrDefault();
                if (nextCycle != null && nextCycle.Item2 < (now + TimeSpan.FromMinutes(5)))
                {
                    // Don't send
                    sendNow = false;
                }
            }

            if (sendNow)
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

                Manager.GetService<INotificationService>().SendMail("Giardino irrigato", body);
                _mailData.Clear();
            }
        }

        private string GetZoneNames(int[] index)
        {
            return string.Join(", ", index.Select(i => GetZoneName(i)));
        }

        private string GetZoneName(int index)
        {
            if (index < _zoneNames.Length)
            {
                return _zoneNames[index];
            }
            else
            {
                return index.ToString();
            }
        }
    }
}

