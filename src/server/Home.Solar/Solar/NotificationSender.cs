using Lucky.Db;
using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;

namespace Lucky.Home.Solar
{
    internal class NotificationSender
    {
        private bool _isSummarySent = true;
        private readonly ITimeSeries<PowerData, DayPowerData> database;

        private ILogger Logger { get; }

        public NotificationSender(InverterDevice inverterDevice, ITimeSeries<PowerData, DayPowerData> database)
        {
            this.database = database;
            Logger = Manager.GetService<ILoggerFactory>().Create("NotifSvc");
            inverterDevice.StateChanged += (o, e) => HandleStateChanged(inverterDevice.State);
        }

        private void HandleStateChanged(InverterState state)
        {
            // From connected/connecting to OFF mean end of the day
            if (state == InverterState.Off)
            {
                // Send summary
                var summary = database.GetAggregatedData();
                // Skip the first migration from day to night at startup during night
                if (summary != null && !_isSummarySent)
                {
                    SendSummaryMail(summary);
                    _isSummarySent = true;
                }
            }
        }

        public void OnNewData()
        {
            _isSummarySent = false;
        }

        private void SendSummaryMail(DayPowerData day)
        {
            var title = string.Format(Resources.solar_daily_summary_title, day.PowerKWh);
            var body = Resources.solar_daily_summary
                    .Replace("{PowerKWh}", day.PowerKWh.ToString("0.0"))
                    .Replace("{PeakPowerW}", day.PeakPowerW.ToString())
                    .Replace("{PeakPowerTimestamp}", day.FromInvariantTime(day.PeakPowerTimestamp).ToString("hh\\:mm\\:ss"))
                    .Replace("{PeakVoltageV}", day.PeakVoltageV.ToString())
                    .Replace("{PeakVoltageTimestamp}", day.FromInvariantTime(day.PeakVoltageTimestamp).ToString("hh\\:mm\\:ss"))
                    .Replace("{SunTime}", (day.Last - day.First).ToString(Resources.solar_daylight_format));

            Manager.GetService<INotificationService>().SendMail(title, body, false);
            Logger.Log("DailyMailSent", "Power", day.PowerKWh);
        }
    }
}
