using System;
using System.Threading.Tasks;

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

        public async Task<double?> ReadData()
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
