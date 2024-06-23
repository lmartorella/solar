using Lucky.Db;
using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using System;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// Logs solar power immediate readings and stats.
    /// Manages csv files as DB.
    /// </summary>
    class DataLogger
    {
        private readonly ITimeSeries<PowerData, DayPowerData> Database;
        private string _lastFault = null;
        private DateTime? _lastFaultMessageTimeStamp;
        private readonly NotificationSender notificationSender;
        private double? _lastAmmeterValue = null;
        private NightState nightState;

        public DataLogger(InverterDevice inverterDevice, AnalogIntegrator ammeterSink, NotificationSender notificationSender, ITimeSeries<PowerData, DayPowerData> database)
        {
            this.notificationSender = notificationSender;
            Database = database;
            inverterDevice.NewData += (o, e) => HandleNewData(e);
            inverterDevice.NightStateChanged += (o, e) => HandleNightStateChanged(e);
            nightState = inverterDevice.NightState;
            ammeterSink.DataChanged += (o, e) => UpdateAmmeterValue(ammeterSink.Data);
        }

        private void HandleNightStateChanged(NightState e)
        {
            nightState = e;
        }

        private void UpdateAmmeterValue(double? data)
        {
            _lastAmmeterValue = data;
        }

        private void HandleNewData(PowerData data)
        {
            // Don't log OFF states
            if (nightState == NightState.Night)
            {
                return;
            }
            // Use the current grid voltage to calculate Net Energy Metering
            if (data.GridVoltageV > 0)
            {
                data.HomeUsageCurrentA = _lastAmmeterValue ?? -1;
            }
            var db = Database;
            if (db != null)
            {
                db.AddNewSample(data);
                if (data.PowerW > 0)
                {
                    // New data, unlock next mail
                    notificationSender.OnNewData();
                }

                CheckFault(data.InverterState);
            }
        }

        public PowerData ImmediateData { get; private set; }

        private void CheckFault(InverterState inverterState)
        {
            var fault = inverterState.IsFaultToNotify();
            if (_lastFault != fault)
            {
                var notification = Manager.GetService<INotificationService>();
                DateTime ts = DateTime.Now;
                if (fault != null)
                {
                    // Enter fault
                    notification.EnqueueStatusUpdate(Resources.solar_error_mail_title, ts.ToString("HH:mm:ss") + ": " + string.Format(Resources.solar_error_mail_error, fault));
                    _lastFaultMessageTimeStamp = ts;
                }
                else
                {
                    // Recover
                    // Try to recover last message update
                    notification.EnqueueStatusUpdate(
                        Resources.solar_error_mail_title, 
                        ts.ToString("HH:mm:ss") + ": " + Resources.solar_error_mail_normal,
                        _lastFaultMessageTimeStamp.HasValue ?
                            string.Format(Resources.solar_error_mail_error_solved, (int)(ts - _lastFaultMessageTimeStamp.Value).TotalSeconds) :
                            null
                    );
                }
                _lastFault = fault;
            }
        }

        public PowerData GetLastSample()
        {
            return Database?.GetLastSample();
        }

        public DayPowerData GetAggregatedData()
        {
            return Database?.GetAggregatedData();
        }
    }
}
