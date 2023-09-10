using Lucky.Home;
using Lucky.Home.Db;
using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Home.Solar
{
    class Program
    {
        private static event Action _dayRotation;
        private static DateTime _nextPeriodStart;
        private static readonly TimeSpan PeriodLenght = TimeSpan.FromDays(1);
        private static Timer _timerMinute;

        //private const string DeviceHostName = "esp32.";
        private const string DeviceHostName = "localhost";

        public static async Task Main(string[] arguments)
        {
            await Bootstrap.Start(arguments, "solar");

            var ammeter = new AnalogIntegratorRpc();
            var inverter = new InverterDevice();
            var dataLogger = new DataLogger(inverter, ammeter);
            var userInterface = new UserInterface(dataLogger, inverter, ammeter);

            var db = new FsTimeSeries<PowerData, DayPowerData>("SOLAR");
            await db.Init(DateTime.Now);
            _dayRotation += async () => await db.Rotate(DateTime.Now);

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

            dataLogger.Init(db);

            new Zcs6000TlmV3(DeviceHostName);

            var pollStrategyManager = Manager.GetService<PollStrategyManager>();
            await pollStrategyManager.StartLoop();
        }
    }
}
