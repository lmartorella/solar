using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using System.Threading.Tasks;

namespace Lucky.Home.Solar
{
    class UserInterface
    {
        /// <summary>
        /// This will never resets, and keep track of the last sampled grid voltage. Used even during night by home ammeter
        /// </summary>
        private double _lastPanelVoltageV = -1.0;

        private readonly AnalogIntegratorRpc ammeterSink;
        private readonly InverterDevice inverterDevice;
        private readonly DataLogger dataLogger;
        private bool isInverterOnline;

        public UserInterface(DataLogger dataLogger, InverterDevice inverterDevice, AnalogIntegratorRpc ammeterSink)
        {
            this.dataLogger = dataLogger;
            this.ammeterSink = ammeterSink;
            this.inverterDevice = inverterDevice;
            inverterDevice.NewData += (o, e) => HandleNewData(e);
            inverterDevice.StateChanged += (o, e) => UpdateState(inverterDevice.State);
            UpdateState(inverterDevice.State);

            _ = Subscribe();
        }

        private void UpdateState(PollStrategyManager.StateEnum state)
        {
            isInverterOnline = state == PollStrategyManager.StateEnum.NightMode || state == PollStrategyManager.StateEnum.Online;
        }

        private async Task Subscribe()
        {
            await Manager.GetService<MqttService>().SubscribeJsonRpc<RpcVoid, SolarRpcResponse>("solar/getStatus", _ =>
            {
                return GetPvData();
            });
        }

        private void HandleNewData(PowerData data)
        {
            if (data.GridVoltageV > 0)
            {
                _lastPanelVoltageV = data.GridVoltageV;
            }
        }

        /// <summary>
        /// Called by web GUI
        /// </summary>
        private async Task<SolarRpcResponse> GetPvData()
        {
            var ret = new SolarRpcResponse
            {
                Status = OnlineStatus
            };
            var lastSample = dataLogger.GetLastSample();
            if (lastSample != null)
            {
                ret.CurrentW = lastSample.PowerW;
                ret.CurrentTs = lastSample.FromInvariantTime(lastSample.TimeStamp).ToString("F");
                ret.TotalDayWh = lastSample.EnergyTodayWh;
                ret.TotalKwh = lastSample.TotalEnergyKWh;
                ret.InverterState = lastSample.InverterState;

                // From a recover boot 
                if (_lastPanelVoltageV <= 0 && lastSample.GridVoltageV > 0)
                {
                    _lastPanelVoltageV = lastSample.GridVoltageV;
                }

                // Find the peak power
                var dayData = dataLogger.GetAggregatedData();
                if (dayData != null)
                {
                    ret.PeakW = dayData.PeakPowerW;
                    ret.PeakTsTime = dayData.FromInvariantTime(dayData.PeakTimestamp).ToString("hh\\:mm\\:ss");
                }
            }

            if (lastSample?.GridVoltageV > 0)
            {
                ret.GridV = lastSample.GridVoltageV;
                ret.UsageA = lastSample.HomeUsageCurrentA;
            }
            else if (_lastPanelVoltageV > 0)
            {
                // APPROX: Use last panel voltage with up-to-date home power usage
                ret.GridV = _lastPanelVoltageV;
                ret.UsageA = (await ammeterSink.ReadData()) ?? -1.0;
            }
            else
            {
                ret.GridV = -1;
                ret.UsageA = -1.0;
            }

            return ret;
        }

        private OnlineStatus OnlineStatus
        {
            get
            {
                if (ammeterSink.IsOnline && isInverterOnline)
                {
                    return OnlineStatus.Online;
                }
                if (!ammeterSink.IsOnline && !isInverterOnline)
                {
                    return OnlineStatus.Offline;
                }
                return OnlineStatus.PartiallyOnline;
            }
        }
    }
}
