using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;

namespace Lucky.Home.Solar
{
    class UserInterface
    {
        /// <summary>
        /// This will never resets, and keep track of the last sampled grid voltage. Used even during night by home ammeter
        /// </summary>s
        private double _lastPanelVoltageV = -1.0;
        private double? _lastAmmeterValue = null;

        private readonly MqttService mqttService;
        private readonly DataLogger dataLogger;
        private bool isInverterOnline;

        public const string Topic = "ui/solar";
        public const string WillPayload = "null";

        public UserInterface(DataLogger dataLogger, InverterDevice inverterDevice, AnalogIntegrator ammeterSink)
        {
            this.dataLogger = dataLogger;
            mqttService = Manager.GetService<MqttService>();
            inverterDevice.NewData += (o, e) => HandleNewInverterData(e);
            inverterDevice.StateChanged += (o, e) => UpdateInverterState(inverterDevice.State);
            ammeterSink.DataChanged += (o, e) => UpdateAmmeterValue(ammeterSink.Data);
            UpdateInverterState(inverterDevice.State);
        }

        private void UpdateAmmeterValue(double? data)
        {
            _lastAmmeterValue = data;
            PublishUpdate();
        }

        private void UpdateInverterState(PollStrategyManager.StateEnum state)
        {
            isInverterOnline = state == PollStrategyManager.StateEnum.NightMode || state == PollStrategyManager.StateEnum.Online;
            PublishUpdate();
        }

        private void HandleNewInverterData(PowerData data)
        {
            if (data.GridVoltageV > 0)
            {
                _lastPanelVoltageV = data.GridVoltageV;
            }
            PublishUpdate();
        }

        /// <summary>
        /// For the web GUI
        /// </summary>
        private void PublishUpdate()
        {
            var packet = new SolarRpcResponse
            {
                Status = OnlineStatus
            };
            var lastSample = dataLogger.GetLastSample();
            if (lastSample != null)
            {
                packet.CurrentW = lastSample.PowerW;
                packet.CurrentTs = lastSample.FromInvariantTime(lastSample.TimeStamp).ToString("F");
                packet.TotalDayWh = lastSample.EnergyTodayWh;
                packet.TotalKwh = lastSample.TotalEnergyKWh;
                packet.InverterState = lastSample.InverterState;

                // From a recover boot 
                if (_lastPanelVoltageV <= 0 && lastSample.GridVoltageV > 0)
                {
                    _lastPanelVoltageV = lastSample.GridVoltageV;
                }

                // Find the peak power
                var dayData = dataLogger.GetAggregatedData();
                if (dayData != null)
                {
                    packet.PeakW = dayData.PeakPowerW;
                    packet.PeakTsTime = dayData.FromInvariantTime(dayData.PeakTimestamp).ToString("hh\\:mm\\:ss");
                }
            }

            if (lastSample?.GridVoltageV > 0)
            {
                packet.GridV = lastSample.GridVoltageV;
                packet.UsageA = lastSample.HomeUsageCurrentA;
            }
            else if (_lastPanelVoltageV > 0)
            {
                // APPROX: Use last panel voltage with up-to-date home power usage
                packet.GridV = _lastPanelVoltageV;
                packet.UsageA = _lastAmmeterValue ?? -1.0;
            }
            else
            {
                packet.GridV = -1;
                packet.UsageA = -1.0;
            }

            mqttService.JsonPublish(Topic, packet);
        }

        private OnlineStatus OnlineStatus
        {
            get
            {
                if (_lastAmmeterValue.HasValue && isInverterOnline)
                {
                    return OnlineStatus.Online;
                }
                if (!_lastAmmeterValue.HasValue && !isInverterOnline)
                {
                    return OnlineStatus.Offline;
                }
                return OnlineStatus.PartiallyOnline;
            }
        }
    }
}
