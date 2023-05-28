using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Solar
{
    class HalfDuplexLineRpc : BaseRpc
    {
        private MqttService mqttService;
        private Task<MqttService.RpcOriginator> rpcSend;
        private Task<MqttService.RpcOriginator> rpcPost;

        public HalfDuplexLineRpc()
        {
            mqttService = Manager.GetService<MqttService>();
            rpcSend = mqttService.RegisterRpcOriginator("samil_0/send", AnalogIntegratorRpc.Timeout + TimeSpan.FromSeconds(1));
            rpcPost = mqttService.RegisterRpcOriginator("samil_0/post", AnalogIntegratorRpc.Timeout + TimeSpan.FromSeconds(1));
        }

        public async Task<Tuple<byte[], string>> SendReceive(byte[] txData, bool wantsResponse)
        {
            try
            {
                var response = await (await (wantsResponse ? rpcSend : rpcPost)).RawRemoteCall(txData);
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
