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
        private InverterState inverterState;

        public const string Topic = "ui/solar";
        public const string WillPayload = "null";

        public UserInterface(DataLogger dataLogger, InverterDevice inverterDevice, AnalogIntegrator ammeterSink)
        {
            this.dataLogger = dataLogger;
            mqttService = Manager.GetService<MqttService>();
            inverterDevice.NewData += (o, e) => HandleNewInverterData(e);
            inverterDevice.StateChanged += (o, e) => UpdateInverterState(inverterDevice.State);
            ammeterSink.HomeDataChanged += (o, e) => UpdateAmmeterValue(ammeterSink.HomeData);
            UpdateInverterState(inverterDevice.State);
        }

        private void UpdateAmmeterValue(double? data)
        {
            _lastAmmeterValue = data;
            PublishUpdate();
        }

        private void UpdateInverterState(InverterState state)
        {
            inverterState = state;
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
                // If the inverter state is OFF, override the data of the last known sample
                packet.InverterState = inverterState == InverterState.Off ? InverterStates.Off : lastSample.InverterState;

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
                    packet.PeakWTs = dayData.FromInvariantTime(dayData.PeakPowerTimestamp).ToString("hh\\:mm\\:ss");
                    packet.PeakV = dayData.PeakVoltageV;
                    packet.PeakVTs = dayData.FromInvariantTime(dayData.PeakVoltageTimestamp).ToString("hh\\:mm\\:ss");
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
                // InverterState.ModbusConnecting means modbus server down. Other states means modbus up
                if (_lastAmmeterValue.HasValue && inverterState == InverterState.Online)
                {
                    return OnlineStatus.Online;
                }
                if (!_lastAmmeterValue.HasValue && inverterState != InverterState.Online)
                {
                    return OnlineStatus.Offline;
                }
                return OnlineStatus.PartiallyOnline;
            }
        }
    }
}
