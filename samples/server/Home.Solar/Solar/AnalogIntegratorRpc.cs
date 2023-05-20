using Lucky.Home.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Solar
{
    class AnalogIntegratorRpc : BaseRpc
    {
        private MqttService mqttService;

        public AnalogIntegratorRpc()
        {
            mqttService = Manager.GetService<MqttService>();
            _ = mqttService.RegisterRemoteCalls(new[] { "ammeter_0/value" });
        }

        public async Task<double?> ReadData()
        {
            try
            {
                var response = await Manager.GetService<MqttService>().RawRemoteCall("ammeter_0/value");
                if (response == null || response.Length == 0)
                {
                    IsOnline = false;
                    return null;
                }
                IsOnline = true;
                return double.Parse(Encoding.UTF8.GetString(response));
            }
            catch (TaskCanceledException)
            {
                // No data
                IsOnline = false;
                return null;
            }
        }
    }
}
