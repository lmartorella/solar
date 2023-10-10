using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Device.Sofar
{
    class PollStrategyManager
    {
        private InverterState state = InverterState.Off;
        private DateTime _lastValidData = DateTime.Now;
        private CommunicationError? _lastCommunicationError = CommunicationError.None;

        /// <summary>
        /// After this time of no samples, enter night mode
        /// </summary>
#if !DEBUG
        private static readonly TimeSpan EnterNightModeAfter = TimeSpan.FromMinutes(5);
#else
        private static readonly TimeSpan EnterNightModeAfter = TimeSpan.FromSeconds(15);
#endif

        /// <summary>
        /// During day (e.g. when samples are working), retry every 10 seconds
        /// </summary>
        private static readonly TimeSpan CheckConnectionPeriodDay = TimeSpan.FromSeconds(10); // Less than grace time

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

        public async Task StartLoop()
        {
            // Start wait loop
            while (true)
            {
                TimeSpan timeout;
                switch (state)
                {
                    case InverterState.Online:
                        timeout = PollDataPeriod;
                        break;
                    case InverterState.ModbusConnecting:
                        timeout = CheckConnectionPeriodDay;
                        break;
                    case InverterState.Off:
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
            /// Send the strategy state, to update the MQTT
            /// </summary>
            public InverterState State;

            /// <summary>
            /// Tell the caller to wait
            /// </summary>
            public Task Task;

            /// <summary>
            /// Modbus bridge TCP is connected? (even if inverter is not responsive).
            /// When the bridge is powered by the inverter itself, during night the connection will be lost.
            /// </summary>
            public bool IsModbusConnected;

            /// <summary>
            /// Has the communication with the inverter failed?
            /// </summary>
            public CommunicationError CommunicationError;
        }

        public PollStrategyManager()
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("PollStrategy");
        }

        public event EventHandler<PullDataEventArgs> PullData;

        public InverterState InverterState
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

        public ILogger Logger { get; }

        private async Task PullNow()
        {
            // Ask data to inverter via MODBUS. The inverter will update the MQTT in case of good data.
            var args = new PullDataEventArgs { State = InverterState };
            PullData?.Invoke(this, args);
            if (args.Task != null)
            {
                await args.Task;
            }

            if (!args.IsModbusConnected)
            {
                InverterState = InverterState.ModbusConnecting;
            }
            SetLastCommunicationError(args.CommunicationError);
            if (args.CommunicationError != CommunicationError.None)
            {
                if (DateTime.Now - _lastValidData > EnterNightModeAfter && args.CommunicationError == CommunicationError.TotalLoss)
                {
                    InverterState = InverterState.Off;
                }
            }
            else
            {
                InverterState = InverterState.Online;
                _lastValidData = DateTime.Now;
            }
        }

        private void SetLastCommunicationError(CommunicationError value)
        {
            if (_lastCommunicationError != value)
            {
                Logger.Log("InvertCommErr", "value", value, "from", _lastCommunicationError);
                _lastCommunicationError = value;
            }
        }
    }
}
