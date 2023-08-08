using Lucky.Db;
using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// Logs solar power immediate readings and stats.
    /// Manages csv files as DB.
    /// </summary>
    class DataLogger
    {
        private bool _isSummarySent = true;
        private ITimeSeries<PowerData, DayPowerData> Database { get; set; }
        private readonly AnalogIntegratorRpc ammeterSink;
        private readonly InverterDevice inverterDevice;
        private string _lastFault = null;
        private IStatusUpdate _lastFaultMessage;
        private readonly ILogger Logger;

        /// <summary>
        /// This will never resets, and keep track of the last sampled grid voltage. Used even during night by home ammeter
        /// </summary>
        private double _lastPanelVoltageV = -1.0;

        public DataLogger(AnalogIntegratorRpc ammeterSink)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("DataLogger");
            this.ammeterSink = ammeterSink;
            inverterDevice = new InverterDevice();
            inverterDevice.NewData += (o, e) => _ = HandleNewData(e);
            inverterDevice.IsStateChanged += (o, e) => HandleStateChanged(inverterDevice.State);
        }

        private async Task HandleNewData(PowerData data)
        {
            // Use the current grid voltage to calculate Net Energy Metering
            if (data.GridVoltageV > 0)
            {
                data.HomeUsageCurrentA = (await ammeterSink.ReadData()) ?? -1;
            }
            var db = Database;
            if (db != null)
            {
                db.AddNewSample(data);
                if (data.PowerW > 0)
                {
                    // New data, unlock next mail
                    _isSummarySent = false;
                }

                CheckFault(data.InverterState);
            }

            if (data.GridVoltageV > 0)
            {
                _lastPanelVoltageV = data.GridVoltageV;
            }
        }

        private void HandleStateChanged(PollStrategyManager.StateEnum state)
        {
            if (state == PollStrategyManager.StateEnum.NightMode)
            {
                // Send summary
                var summary = Database.GetAggregatedData();
                // Skip the first migration from day to night at startup during night
                if (summary != null && !_isSummarySent)
                {
                    SendSummaryMail(summary);
                    _isSummarySent = true;
                }
            }
        }

        public void Init(ITimeSeries<PowerData, DayPowerData> database)
        {
            Database = database;
        }

        public PowerData ImmediateData { get; private set; }

        private void CheckFault(string inverterState)
        {
            var fault = InverterStates.ToFault(inverterState);
            if (_lastFault != fault)
            {
                var notification = Manager.GetService<INotificationService>();
                DateTime ts = DateTime.Now;
                if (fault != null)
                {
                    _lastFaultMessage = notification.EnqueueStatusUpdate("Errori Inverter", "Errore: " + fault);
                }
                else
                {
                    // Try to recover last message update
                    bool notify = true;
                    if (_lastFaultMessage != null)
                    {
                        if (_lastFaultMessage.Update(() =>
                        {
                            _lastFaultMessage.Text += ", risolto dopo " + (int)(DateTime.Now - _lastFaultMessage.TimeStamp).TotalSeconds + " secondi.";
                        }))
                        {
                            notify = false;
                        }
                    }
                    if (notify)
                    {
                        notification.EnqueueStatusUpdate("Errori Inverter", "Normale");
                    }
                }
                _lastFault = fault;
            }
        }

        private void SendSummaryMail(DayPowerData day)
        {
            var title = string.Format(Resources.solar_daily_summary_title, day.PowerKWh);
            var body = Resources.solar_daily_summary
                    .Replace("{PowerKWh}", day.PowerKWh.ToString("0.0"))
                    .Replace("{PeakPowerW}", day.PeakPowerW.ToString())
                    .Replace("{PeakTimestamp}", day.FromInvariantTime(day.PeakTimestamp).ToString("hh\\:mm\\:ss"))
                    .Replace("{SunTime}", (day.Last - day.First).ToString(Resources.solar_daylight_format));

            Manager.GetService<INotificationService>().SendMail(title, body, false);
            Logger.Log("DailyMailSent", "Power", day.PowerKWh);
        }

        private OnlineStatus OnlineStatus
        {
            get
            {
                var sinks = new BaseDevice[] { ammeterSink, inverterDevice };
                if (sinks.All(s => s.IsOnline))
                {
                    return OnlineStatus.Online;
                }
                if (sinks.All(s => !s.IsOnline))
                {
                    return OnlineStatus.Offline;
                }
                return OnlineStatus.PartiallyOnline;
            }
        }

        /// <summary>
        /// Called by web GUI
        /// </summary>
        private async Task<SolarRpcResponse> GetPvData() 
        {
            var ret = new SolarRpcResponse
            {
                Status = OnlineStatus
            };
            var lastSample = Database?.GetLastSample();
            if (lastSample != null) {
                ret.CurrentW = lastSample.PowerW;
                ret.CurrentTs = lastSample.FromInvariantTime(lastSample.TimeStamp).ToString("F");
                ret.TotalDayWh = lastSample.EnergyTodayWh;
                ret.TotalKwh = lastSample.TotalEnergyKWh; 
                ret.InverterState = lastSample.InverterState;

                // From a recover boot 
                if (_lastPanelVoltageV <= 0 && lastSample.GridVoltageV > 0)
                {
                    _lastPanelVoltageV = lastSample.GridVoltageV;
                }

                // Find the peak power
                var dayData = Database.GetAggregatedData();
                if (dayData != null)
                {
                    ret.PeakW = dayData.PeakPowerW;
                    ret.PeakTsTime = dayData.FromInvariantTime(dayData.PeakTimestamp).ToString("hh\\:mm\\:ss");
                }
            }

            if (lastSample?.GridVoltageV > 0)
            {
                ret.GridV = lastSample.GridVoltageV;
                ret.UsageA = lastSample.HomeUsageCurrentA;
            }
            else if (_lastPanelVoltageV > 0)
            {
                // APPROX: Use last panel voltage with up-to-date home power usage
                ret.GridV = _lastPanelVoltageV;
                ret.UsageA = (await ammeterSink.ReadData()) ?? -1.0;
            }
            else
            {
                ret.GridV = -1;
                ret.UsageA = -1.0;
            }

            return ret;
        }
    }
}
