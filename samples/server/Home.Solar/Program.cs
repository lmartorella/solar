using Lucky.Home;
using Lucky.Home.Db;
using Lucky.Home.Devices.Solar;
using Lucky.Home.Power;
using Lucky.Home.Services;
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

        public static async Task Main(string[] arguments)
        {
            await Bootstrap.Start(arguments, "solar");

            var inverter = new HalfDuplexLineRpc();
            var ammeter = new AnalogIntegratorRpc();
            var device = new SamilInverterLoggerDevice(inverter, ammeter);

            var db = new FsTimeSeries<PowerData, DayPowerData>(device.Name);
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

            Manager.Killed += (o, e) => device.OnTerminate();
            await device.StartLoop(db);
        }
    }
}
