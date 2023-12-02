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
        /// Decimal value of Ampere RMS, home usage
        /// </summary>
        public const string HomeDataTopicId = "ammeter/home";

        /// <summary>
        /// Decimal value of Ampere RMS, grid immission
        /// </summary>
        public const string ImmissionDataTopicId = "ammeter/immission";

        public AnalogIntegrator()
        {
            mqttService = Manager.GetService<MqttService>();
            // The ammeter uses will to send zero byte packet when disconnected
            _ = mqttService.SubscribeRawTopic(HomeDataTopicId, HandleHomeData);
        }

        private void HandleHomeData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                HomeData = null;
            }
            else
            {
                HomeData = double.Parse(Encoding.UTF8.GetString(data));
            }
        }

        /// <summary>
        /// Event raised when new home usage data comes from the sensor, or the state changes
        /// </summary>
        public event EventHandler HomeDataChanged;

        /// <summary>
        /// Last sample of home usage. Null means offline
        /// </summary>
        public double? HomeData
        {
            get => data;
            set
            {
                if (data != value)
                {
                    data = value;
                    HomeDataChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
