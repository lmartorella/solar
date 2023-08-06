using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Device.Sofar
{
    class PollStrategyManager
    {
        private StateEnum state = StateEnum.NightMode;
        private DateTime _lastValidData = DateTime.Now;
        private readonly ILogger Logger;

        public enum StateEnum
        {
            NightMode,
            Connecting,
            Online
        }

        /// <summary>
        /// After this time of no samples, enter night mode
        /// </summary>
#if !DEBUG
        private static readonly TimeSpan EnterNightModeAfter = TimeSpan.FromMinutes(2);
#else
        private static readonly TimeSpan EnterNightModeAfter = TimeSpan.FromSeconds(15);
#endif

        /// <summary>
        /// During day (e.g. when samples are working), retry every 10 seconds
        /// </summary>
        private static readonly TimeSpan CheckConnectionPeriodDay = TimeSpan.FromSeconds(10); // Less than grace time
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// During night (e.g. when last sample is older that 2 minutes), retry every 2 minutes, to give time to the inverter to 
        /// be stable good enough
        /// </summary>
#if !DEBUG
        private static readonly TimeSpan CheckConnectionPeriodNight = TimeSpan.FromMinutes(2);
#else
        private static readonly TimeSpan CheckConnectionPeriodNight = TimeSpan.FromSeconds(15);
#endif

        /// <summary>
        /// Get a solar PV sample every 15 seconds
        /// </summary>
        private static readonly TimeSpan PollDataPeriod = TimeSpan.FromSeconds(15);

        public PollStrategyManager()
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("PollMgr");
            _ = StartLoop();
        }

        private async Task StartLoop()
        {
            // Start wait loop
            while (true)
            {
                TimeSpan timeout;
                switch (state)
                {
                    case StateEnum.Online:
                        timeout = PollDataPeriod;
                        break;
                    case StateEnum.Connecting:
                        timeout = CheckConnectionPeriodDay;
                        break;
                    case StateEnum.NightMode:
                    default:
                        timeout = CheckConnectionPeriodNight;
                        break;
                }
                await Task.Delay(timeout);
                await PullNow();
            }
        }

        public class PullDataEventArgs
        {
            /// <summary>
            /// Send the strategy state
            /// </summary>
            public StateEnum State;

            /// <summary>
            /// Tell the caller to wait
            /// </summary>
            public Task Task;

            public TimeSpan ConnectTimeout;

            /// <summary>
            /// Modbus bridge TCP is connected? (even if inverter is not responsive).
            /// When the bridge is powered by the inverter itself, during night the connection will be lost.
            /// </summary>
            public bool IsConnected;

            /// <summary>
            /// Data valid?
            /// </summary>
            public bool DataValid;
        }

        public event EventHandler<PullDataEventArgs> PullData;

        public StateEnum State
        {
            get
            {
                return state;
            }
            set
            {
                if (state != value)
                {
                    state = value;
                    Logger.Log("State: " + value);
                }
            }
        }

        private async Task PullNow()
        {
            // Ask data to inverter via MODBUS. The inverter will update the MQTT in case of good data.
            var args = new PullDataEventArgs { State = State, ConnectTimeout = ConnectTimeout };
            PullData?.Invoke(this, args);
            if (args.Task != null)
            {
                await args.Task;
            }

            if (!args.IsConnected)
            {
                State = StateEnum.Connecting;
            }
            if (!args.DataValid)
            {
                if (DateTime.Now - _lastValidData > EnterNightModeAfter)
                {
                    State = StateEnum.NightMode;
                }
            }
            else
            {
                State = StateEnum.Online;
                _lastValidData = DateTime.Now;
            }
        }
    }
}
