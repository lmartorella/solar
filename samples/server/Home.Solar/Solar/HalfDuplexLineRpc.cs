using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Solar
{
    class HalfDuplexLineRpc : BaseRpc
    {
        private MqttService mqttService;

        public HalfDuplexLineRpc()
        {
            mqttService = Manager.GetService<MqttService>();
            _ = mqttService.RegisterRemoteCalls(new[] { "samil_0/send", "samil_0/post" });
        }

        public async Task<Tuple<byte[], string>> SendReceive(byte[] txData, bool wantsResponse)
        {
            try
            {
                var response = await Manager.GetService<MqttService>().RawRemoteCall(wantsResponse ? "samil_0/send" : "samil_0/post", txData);
                IsOnline = (response != null && response.Length > 0);
                return Tuple.Create(response, null as string);
            }
            catch (MqttRemoteCallError err)
            {
                // Managed error about the inverter
                IsOnline = true;
                return Tuple.Create<byte[], string>(null, err.Message);
            }
            catch (TaskCanceledException)
            {
                // No data
                IsOnline = false;
                return Tuple.Create<byte[], string>(null, null);
            }
        }
    }
}
