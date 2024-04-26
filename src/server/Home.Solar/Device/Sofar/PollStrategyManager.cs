using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Device.Sofar
{
    class PollStrategyManager
    {
        private NightState nightState = NightState.Night;
        private DateTime _lastValidData = DateTime.Now;
        private CommunicationError? _lastCommunicationError = CommunicationError.None;
        private bool isConnected;

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
        /// During night (e.g. when last sample is older that 5 minutes), retry less often, to give time to the inverter to 
        /// be stable good enough
        /// </summary>
        private static readonly TimeSpan CheckConnectionPeriodNight = TimeSpan.FromSeconds(25);

        /// <summary>
        /// Get a solar PV sample every 15 seconds
        /// </summary>
#if !DEBUG
        private static readonly TimeSpan PollDataPeriod = TimeSpan.FromSeconds(15);
#else
        private static readonly TimeSpan PollDataPeriod = TimeSpan.FromSeconds(5);
#endif

        public async Task StartLoop()
        {
            // Start wait loop
            while (true)
            {
                await PullNow();
                TimeSpan timeout;
                switch (nightState)
                {
                    case NightState.Running:
                        timeout = PollDataPeriod;
                        break;
                    case NightState.Night:
                    default:
                        if (isConnected)
                        {
                            timeout = CheckConnectionPeriodDay;
                        }
                        else
                        {
                            timeout = CheckConnectionPeriodNight;
                        }
                        break;
                }
                await Task.Delay(timeout);
            }
        }

        public class PullDataEventArgs
        {
            /// <summary>
            /// Send the strategy state, to update the MQTT
            /// </summary>
            public NightState NightState;

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

        public NightState NightState
        {
            get
            {
                return nightState;
            }
            set
            {
                if (nightState != value)
                {
                    nightState = value;
                    Logger.Log("NightState: " + value);
                }
            }
        }

        public ILogger Logger { get; }

        private void UpdateOffline(bool isConnected)
        {
            this.isConnected = isConnected;
            if (DateTime.Now - _lastValidData > EnterNightModeAfter)
            {
                NightState = NightState.Night;
            }
        }

        private async Task PullNow()
        {
            // Ask data to inverter via MODBUS. The inverter will update the MQTT in case of good data.
            var args = new PullDataEventArgs { NightState = NightState };
            PullData?.Invoke(this, args);
            if (args.Task != null)
            {
                await args.Task;
            }

            if (!args.IsModbusConnected)
            {
                UpdateOffline(false);
            }
            else
            {
                SetLastCommunicationError(args.CommunicationError);
                switch (args.CommunicationError)
                {
                    case CommunicationError.None:
                        NightState = NightState.Running;
                        _lastValidData = DateTime.Now;
                        break;
                    case CommunicationError.PartialLoss:
                        NightState = NightState.Running;
                        break;
                    case CommunicationError.TotalLoss:
                        UpdateOffline(true);
                        break;
                    case CommunicationError.ChannelError:
                        UpdateOffline(false);
                        break;
                }
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
