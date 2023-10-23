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
        private IStatusUpdate _lastFaultMessage;
        private readonly NotificationSender notificationSender;
        private readonly ILogger Logger;
        private double? _lastAmmeterValue = null;

        public DataLogger(InverterDevice inverterDevice, AnalogIntegrator ammeterSink, NotificationSender notificationSender, ITimeSeries<PowerData, DayPowerData> database)
        {
            this.notificationSender = notificationSender;
            Database = database;
            Logger = Manager.GetService<ILoggerFactory>().Create("DataLogger");
            inverterDevice.NewData += (o, e) => HandleNewData(e);
            ammeterSink.DataChanged += (o, e) => UpdateAmmeterValue(ammeterSink.Data);
        }

        private void UpdateAmmeterValue(double? data)
        {
            _lastAmmeterValue = data;
        }

        private void HandleNewData(PowerData data)
        {
            // Don't log OFF states
            if (data.InverterState == InverterStates.Off)
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

        private void CheckFault(string inverterState)
        {
            var fault = InverterStates.ToFault(inverterState);
            if (_lastFault != fault)
            {
                var notification = Manager.GetService<INotificationService>();
                DateTime ts = DateTime.Now;
                if (fault != null)
                {
                    _lastFaultMessage = notification.EnqueueStatusUpdate(Resources.solar_error_mail_title, string.Format(Resources.solar_error_mail_error, fault));
                }
                else
                {
                    // Try to recover last message update
                    bool notify = true;
                    if (_lastFaultMessage != null)
                    {
                        if (_lastFaultMessage.Update(() =>
                        {
                            _lastFaultMessage.Text += string.Format(Resources.solar_error_mail_error_solved, (int)(DateTime.Now - _lastFaultMessage.TimeStamp).TotalSeconds);
                        }))
                        {
                            notify = false;
                        }
                    }
                    if (notify)
                    {
                        notification.EnqueueStatusUpdate(Resources.solar_error_mail_title, Resources.solar_error_mail_normal);
                    }
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
