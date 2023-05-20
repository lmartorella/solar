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
            _ = mqttService.RegisterRemoteCalls(new[] { "samil/send", "samil/post" });
        }

        public async Task<Tuple<byte[], string>> SendReceive(byte[] txData, bool wantsResponse)
        {
            try
            {
                var response = await Manager.GetService<MqttService>().RemoteCall(wantsResponse ? "samil/send" : "samil/post", txData);
                IsOnline = true;
                return Tuple.Create(response, null as string);
            }
            catch (MqttRemoteCallError err)
            {
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
