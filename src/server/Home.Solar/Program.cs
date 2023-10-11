using Lucky.Home;
using Lucky.Home.Db;
using Lucky.Home.Device;
using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.Linq;
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

        private const string DeviceHostName = "localhost";

        public static async Task Main(string[] arguments)
        {
            try
            {
                await _Main(arguments);
            }
            catch (Exception exc)
            {
                Manager.GetService<LoggerFactory>().Create("Main").Exception(exc);
            }
        }

        private static async Task _Main(string[] arguments)
        {
            await Bootstrap.Start(arguments, "solar");

            var ammeter = new AnalogIntegrator();
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

            await StartModbusBridges();
        }

        /// <summary>
        /// Start modbus to mqtt bridges
        /// </summary>
        private static async Task StartModbusBridges()
        {
            var configuration = Manager.GetService<SolarConfigurationService>().State;

            var pollStrategyManager = new PollStrategyManager();
            var inverterBridge = new Zcs6000TlmV3(pollStrategyManager, configuration.InverterHostName, configuration.InverterStationId);
            var ammeter = new ModbusAmmeter(configuration.AmmeterHostName, configuration.AmmeterStationId);

            var waitTasks = new[]
            {
                Tuple.Create(inverterBridge.StartLoop(), "Inverter"),
                Tuple.Create(ammeter.StartLoop(), "Ammeter"),
                Tuple.Create(Manager.Run(), "killed")
            };
            var finishedTask = await Task.WhenAny(waitTasks.Select(t => t.Item1));
            Manager.GetService<LoggerFactory>().Create("Main").Log("LoopExited", "task#", waitTasks.First(t => t.Item1 == finishedTask).Item2);
            await finishedTask;
        }
    }
}
