using Lucky.Home.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Solar
{
    class AnalogIntegratorRpc : BaseRpc
    {
        private MqttService mqttService;
        private Task<MqttService.RpcOriginator> rpc;
        public static TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public AnalogIntegratorRpc()
        {
            mqttService = Manager.GetService<MqttService>();
            rpc = mqttService.RegisterRpcOriginator("ammeter_0/value", Timeout);
        }

        public async Task<double?> ReadData()
        {
            try
            {
                var response = await (await rpc).RawRemoteCall();
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
