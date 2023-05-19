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
            _ = mqttService.RegisterRemoteCalls(new[] { "solar/send", "solar/post" });
        }

        public async Task<Tuple<byte[], string>> SendReceive(byte[] txData, bool wantsResponse, string _opName)
        {
            try
            {
                var response = await Manager.GetService<MqttService>().RemoteCall(wantsResponse ? "solar/send" : "solar/post", txData);
                return Tuple.Create(response, null as string);
            }
            catch (MqttRemoveCallError err)
            {
                return Tuple.Create<byte[], string>(null, err.Message);
            }
        }
    }
}
