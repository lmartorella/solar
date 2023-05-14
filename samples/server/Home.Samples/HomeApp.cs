using System;
using Lucky.Home.Services;
using Lucky.Home.Devices;
using System.Linq;
using Lucky.Home.Db;
using System.Threading;
using Lucky.Home.Power;
using System.Threading.Tasks;
using Lucky.Home.Application;
using Lucky.Home;
using Lucky.Home.Devices.Solar;
using Lucky.Home.Devices.Garden;

[assembly: Application(typeof (HomeApp))]

namespace Lucky.Home.Application
{
    /// <summary>
    /// The home application
    /// </summary>
    class HomeApp : ServiceBase, IApplication
    {
        private Timer _timerMinute;
        private event Action _dayRotation;
        private DateTime _nextPeriodStart;
        private static readonly TimeSpan PeriodLenght = TimeSpan.FromDays(1);

        public async Task Start()
        {
            // Get all registered devices
            var deviceMan = Manager.GetService<IDeviceManager>();
            IDevice[] devices = deviceMan.Devices;

            // Process all solar devices
            foreach (var device in devices.OfType<ISolarPanelDevice>())
            {
                var db = new FsTimeSeries<PowerData, DayPowerData>(device.Name);
                await db.Init(DateTime.Now);
                _ = device.StartLoop(db);
                _dayRotation += async () => await db.Rotate(DateTime.Now);
            }

            // Rotate solar db at midnight 
            var periodStart = _nextPeriodStart = DateTime.Now.Date;
            while (DateTime.Now >= _nextPeriodStart)
            {
                _nextPeriodStart += PeriodLenght;
            }
            _timerMinute = new Timer(s => 
            {
                if (DateTime.Now >= _nextPeriodStart)
                {
                    while (DateTime.Now >= _nextPeriodStart)
                    {
                        _nextPeriodStart += PeriodLenght;
                    }
                    _dayRotation?.Invoke();
                }
            }, null, 0, 30 * 1000);

            // If a mail should be sent at startup, do it now
            var mailBody = Manager.GetService<IConfigurationService>().GetConfig("sendMailErr");
            if (!string.IsNullOrEmpty(mailBody))
            {
                // Append err file to the mail body, if avail
                string lastErrorFile = Manager.GetService<ILoggerFactory>().LastErrorText;
                if (lastErrorFile != null)
                {
                    mailBody += Environment.NewLine + Environment.NewLine + lastErrorFile;
                }
                await Manager.GetService<INotificationService>().SendMail(Resources.startupMessage, mailBody, true);
            }

            // Implements a IPC pipe with web server
            Manager.GetService<MqttService>().SubscribeRpc("kill", async (RpcRequest _) =>
            {
                await Task.Delay(1500);
                Manager.Kill(Logger, "killed by parent process");
                return new RpcResponse();
            });
        }
    }
}
