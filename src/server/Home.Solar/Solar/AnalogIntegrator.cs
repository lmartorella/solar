using Lucky.Home.Services;
using System;
using System.Text;

namespace Lucky.Home.Solar
{
    class AnalogIntegrator
    {
        private MqttService mqttService;
        private double? data;

        /// <summary>
        /// RAW float LE data published by the ammeter
        /// </summary>
        public const string DataTopicId = "ammeter_0/value";

        public AnalogIntegrator()
        {
            mqttService = Manager.GetService<MqttService>();
            // The ammeter uses will to send zero byte packet when disconnected
            _ = mqttService.SubscribeRawTopic(DataTopicId, HandleData);
        }

        private void HandleData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Data = null;
            }
            else
            {
                Data = double.Parse(Encoding.UTF8.GetString(data));
            }
        }

        /// <summary>
        /// Event raised when new data comes from the inverter, or the state changes
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Last sample. Null means offline
        /// </summary>
        public double? Data
        {
            get => data;
            set
            {
                if (data != value)
                {
                    data = value;
                    DataChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
