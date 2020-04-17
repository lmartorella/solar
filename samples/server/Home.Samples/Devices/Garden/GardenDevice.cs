using Lucky.Db;
using Lucky.Home.IO;
using Lucky.Home.Model;
using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    /// <summary>
    /// Control and diagnose a custom garden programmer
    /// </summary>
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    [Requires(typeof(FlowSink))]
    public class GardenDevice : DeviceBase
    {
        /// <summary>
        /// Poll for the main cycle
        /// </summary>
        private static TimeSpan MAIN_POLL_PERIOD = TimeSpan.FromSeconds(3);

        private FileInfo _cfgFile;
        private object _timeProgramLock = new object();
        private readonly TimeProgram<GardenCycle> _timeProgram;
        private readonly Queue<ImmediateProgram> _cycleQueue = new Queue<ImmediateProgram>();
        private string[] _zoneNames = new string[0];
        private FileWatcher _fileWatcher;
        private readonly double _counterFq;
        private PumpOperationObserver _pumpOpObserver = new PumpOperationObserver();
        private EventHandler<PipeServer.MessageEventArgs> _pipeMessageHandler;

        public GardenDevice(double counterFq = 5.5)
        {
            _counterFq = counterFq;
            _timeProgram = new TimeProgram<GardenCycle>(Logger);
            _timeProgram.CycleTriggered += HandleProgramCycle;

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
            // Subscribe changes
            _fileWatcher = new FileWatcher(_cfgFile);
            _fileWatcher.Changed += (_o, _e) => ReadConfig();
            ReadConfig();

            // To receive commands from UI
            _pipeMessageHandler = (o, e) =>
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
                                await (GetFirstOnlineSink<GardenSink>()?.UpdateFlowData((int)flowData.FlowLMin) ?? Task.FromResult<string>(null));
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
                        var zones = ((GardenWebRequest)e.Request).ImmediateZones;
                        Logger.Log("setImmediate", "msg", string.Join(",", zones.Select(z => z.ToString())));
                        e.Response = Task.FromResult((WebResponse) new GardenWebResponse
                        {
                            Error = ScheduleCycle(new ImmediateProgram
                            {
                                ZoneTimes = zones.Select(prg => new ZoneTime
                                {
                                    Minutes = prg.Time,
                                    Zones = prg.Zones
                                }).ToArray(),
                                Name = string.Format(Resources.gardenImmediate, string.Join("; ", zones.Select(z => z.ToString("f", _zoneNames))))
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
            Manager.GetService<PipeServer>().Message += _pipeMessageHandler;

            _ = StartLoop();
        }

        protected override void OnSinkChanged(SubSink removed, SubSink added)
        {
            if (removed != null)
            {
                _pumpOpObserver.OnSinkRemoved(removed);
            }
            if (added != null)
            {
                _pumpOpObserver.OnSinkAdded(added);
            }

            base.OnSinkChanged(removed, added);
        }

        internal async Task<FlowData> ReadFlow()
        {
            var flowSink = GetFirstOnlineSink<FlowSink>();
            if (flowSink != null)
            {
                try
                {
                    return await flowSink.ReadData(_counterFq);
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
        private async Task StartLoop()
        {
            RunningProgram runningProgram = null;
            int errors = 0;

            while (!IsDisposed)
            {
                await Task.Delay(MAIN_POLL_PERIOD);

                // Program in progress?
                bool cycleIsWaiting = false;
                // Check for new program to run
                lock (_cycleQueue)
                {
                    cycleIsWaiting = _cycleQueue.Count > 0;
                }

                // Do I need to contact the garden sink?
                if (runningProgram != null || cycleIsWaiting)
                {
                    var gardenSink = GetFirstOnlineSink<GardenSink>();
                    if (gardenSink != null)
                    {
                        // Wait for a free garden device
                        var state = await gardenSink.Read(false);
                        if (state == null)
                        {
                            // Lost connection with garden programmer!
                            if (errors++ < 5)
                            {
                                Logger.Log("Cannot contact garden (2)", "cycleIsWaiting", cycleIsWaiting, "inProgress", runningProgram != null);
                                continue;
                            }
                        }

                        errors = 0;
                        DateTime now = DateTime.Now;
                        if (state.IsAvailable)
                        {
                            // Finished?
                            if (runningProgram != null)
                            {
                                _ = runningProgram.Stop(now);
                                runningProgram = null;
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

                                runningProgram = new RunningProgram(cycle, Logger, this);
                                await runningProgram.Start(now);
                            }
                        }
                        else
                        {
                            if (runningProgram != null)
                            {
                                // The program is running? Log flow
                                await runningProgram.Step(now, state);
                            }
                        }
                    }
                    else
                    {
                        // Lost connection with garden programmer!
                        if (errors++ < 5)
                        {
                            Logger.Log("Cannot contact garden", "cycleIsWaiting", cycleIsWaiting, "inProgress", runningProgram != null);
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
            _fileWatcher.Dispose();
            Manager.GetService<PipeServer>().Message -= _pipeMessageHandler;
            return base.OnTerminate();
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

        internal static byte ToZoneMask(int[] zones)
        {
            byte ret = 0;
            foreach (int zone in zones)
            {
                ret = (byte)(ret | (1 << zone));
            }
            return ret;
        }

        internal string GetZoneName(int index)
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

        internal Tuple<GardenCycle, DateTime> GetNextCycle(DateTime now)
        {
            lock (_timeProgramLock)
            {
                return _timeProgram.GetNextCycles(now).FirstOrDefault();
            }
        }
    }
}

