using Lucky.Home.Services;
using System;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink adapter for Bosch BPM180 sensor
    /// </summary>
    [SinkId("ANIN")]
    class AnalogIntegratorSink : SinkBase
    {
        public class DataMessage
        {
            /// <summary>
            /// This is the factor of an unit, compared to a full lecture (2^10 - 1)
            /// </summary>
            public float Factor;

            public UInt32 Value;

            public UInt16 Count;
        }

        public AnalogIntegratorSink()
        {
            _ = Subscribe();
        }

        private async Task Subscribe()
        {
            await Manager.GetService<MqttService>().SubscribeRawRpcRequest("ammeter_0/value", async payload =>
            {
                double? value = await ReadData();
                if (value != null)
                {
                    return null;
                }
                return Encoding.UTF8.GetBytes(value.ToString());
            });
        }

        /// <summary>
        /// Read data. Returns null if the sample is invalid
        /// </summary>
        private async Task<double?> ReadData()
        {
            double? retValue = null;
            await Read(async reader =>
            {
                var msg = (await reader.Read<DataMessage>());
                if (msg != null)
                {
                    retValue = ((double)msg.Value / msg.Count) * msg.Factor;
                }
            });
            return retValue;
        }
    }
}
