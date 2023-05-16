using Lucky.Home.Db;
using Lucky.Home.Devices.Solar;
using Lucky.Home.Power;
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

        static void Main()
        {
            _ = Start();
        }

        private static async Task Start()
        {
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

            await device.StartLoop(db);
        }
    }
}
