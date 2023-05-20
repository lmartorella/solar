using Lucky.Home.Services;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink for water flow device. Each tick count is a nominal quantity of water (e.g. 5.5 ticks/seconds means 1 liter/minute).
    /// The sink sends both current flow and total amount of water counted.
    /// </summary>
    [SinkId("FLOW")]
    class FlowSink : SinkBase
    {
        /// <summary>
        /// A flow sample
        /// </summary>
        [DataContract]
        public class FlowData
        {
            /// <summary>
            /// Total counter in m3
            /// </summary>
            [DataMember(Name = "totalMc")]
            public double TotalMc;

            /// <summary>
            /// Current flow in liters/minute
            /// </summary>
            [DataMember(Name = "flowLMin")]
            public double FlowLMin;

            [DataMember(Name = "offline")]
            public bool Offline;
        }

        /// <summary>
        /// A flow sample
        /// </summary>
        private class GetDataMessage
        {
            /// <summary>
            /// Total counter
            /// </summary>
            public uint Count;

            /// <summary>
            /// Count in the time unit (count/seconds)
            /// </summary>
            public ushort Frequency;
        }

        public FlowSink()
        {
            _ = Subscribe();
        }

        private async Task Subscribe()
        {
            await Manager.GetService<MqttService>().SubscribeJsonRpc<RpcVoid, FlowData>("flow_meter_0/value", async payload =>
            {
                FlowData data;
                if (!IsOnline)
                {
                    data = new FlowData { Offline = true };
                }
                else
                {
                    data = await ReadData();
                }
                return data;
            });
        }

        /// <summary>
        /// fq is frequency / L/min (5.5 on sample counter)
        /// </summary>
        private async Task<FlowData> ReadData(double fq = 5.5, int timeout = 3000)
        {
            // F(tick/sec) = fq * flow(L / Min)
            // flow(L/min) = F(tick/sec) / fq

            // F(tick/min) = fq * 60 * flow(L/min)
            // ticks = fq * 60 * L
            // L = ticks / fq / 60  
            // Mc = ticks / fq / 60.000

            FlowData data = null;
            await Read(async reader =>
            {
                var rawData = await reader.Read<GetDataMessage>();
                if (rawData != null)
                {
                    data = new FlowData { TotalMc = rawData.Count / fq / 60000.0, FlowLMin = rawData.Frequency / fq };
                }
            }, timeout);
            return data;
        }
    }
}
