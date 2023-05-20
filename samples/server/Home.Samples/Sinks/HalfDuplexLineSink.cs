using Lucky.Home.Serialization;
using Lucky.Home.Services;
using System;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink for half-duplex serial line: 9600,N,1, 0.1 sec RX timeout
    /// </summary>
    [SinkId("SLIN")]
    class HalfDuplexLineSink : SinkBase
    {
        private class Message
        {
            public byte Mode;

            [SerializeAsDynArray]
            public byte[] TxData;
        }

        public enum Error
        {
            Ok,
            Overflow,
            FrameError,

            // ClientError
            ClientNoData = 32
        }

        private class StateMessage
        {
            [SerializeAsDynArray]
            [DynArrayCase(-1, "Error", Error.Overflow)]
            [DynArrayCase(-2, "Error", Error.FrameError)]
            public byte[] Data;

            [NoSerialize]
            public Error Error;
        }

        public HalfDuplexLineSink()
        {
            _ = Subscribe();
        }

        private async Task Subscribe()
        { 
            await Manager.GetService<MqttService>().SubscribeRawRpcRequest("samil/send", async payload =>
            {
                var response = await SendReceive(payload, true);
                if (response.Item2 == Error.Ok)
                {
                    return response.Item1;
                }
                else
                {
                    throw new MqttRemoteCallError(response.Item2.ToString());
                }
            });
            await Manager.GetService<MqttService>().SubscribeRawRpcRequest("samil/post", async payload =>
            {
                var response = await SendReceive(payload, false);
                if (response.Item2 == Error.Ok)
                {
                    return response.Item1;
                }
                else
                {
                    throw new MqttRemoteCallError(response.Item2.ToString());
                }
            });
        }

        private async Task<Tuple<byte[], Error>> SendReceive(byte[] txData, bool wantsResponse)
        {
            byte mode = 0;
            if (!wantsResponse)
            {
                mode = 0xfe;
            }
            await Write(async writer =>
            {
                await writer.Write(new Message { TxData = txData, Mode = mode });
            }, "WR");

            byte[] data = null;
            Error err = Error.Ok;
            // The Read operation is actually sending the buffer first and then synchronously (blocking the bus)
            // reading the response (if mode is not 0xfe)
            await Read(async reader =>
            {
                var msg = await reader.Read<StateMessage>();
                if (msg == null)
                {
                    data = new byte[0];
                    err = Error.ClientNoData;
                }
                else
                {
                    data = msg.Data ?? new byte[0];
                    err = msg.Error;
                }
            }, 0, "RD");
            return Tuple.Create(data, err);
        }
    }
}
