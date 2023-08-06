using Lucky.Home.Device.Sofar;
using Lucky.Home.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// Encapsulate a MQTT topic source
    /// </summary>
    class InverterDevice : BaseDevice
    {
        public const string SolarDataTopicId = "solar/data";
        public const string SolarStateTopicId = "solar/state";

        public InverterDevice()
        {
            _ = Subscribe();
        }

        private async Task Subscribe()
        {
            await Manager.GetService<MqttService>().SubscribeJsonTopic(SolarDataTopicId, (PowerData data) =>
            {
                NewData?.Invoke(this, data);
            });
            await Manager.GetService<MqttService>().SubscribeRawTopic(SolarStateTopicId, data =>
            {
                if (Enum.TryParse(Encoding.UTF8.GetString(data), out State))
                {
                    IsStateChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public event EventHandler<PowerData> NewData;

        public event EventHandler IsStateChanged;

        public PollStrategyManager.StateEnum State;
    }
}
