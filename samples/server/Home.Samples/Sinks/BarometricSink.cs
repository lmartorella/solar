using Lucky.Home.Serialization;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    [SinkId("BM18")]
    class BarometricSink : SinkBase
    {
        private class ModeCommand
        {
            public byte Command;
        }

        private class CalibrationMessage
        {
            [SerializeAsFixedArray(22)]
            public byte[] Data;
        }

        private class RawData
        {
            [SerializeAsFixedArray(5)]
            public byte[] Data;
        }

        public async Task<byte[]> ReadCalibrationData()
        {
            await Write(async writer =>
            {
                await writer.Write(new ModeCommand { Command = 22 });
            });
            byte[] data = null;
            await Read(async reader =>
            {
                data = (await reader.Read<CalibrationMessage>())?.Data;
            });
            return data;
        }

        public async Task<byte[]> ReadUncompensatedData()
        {
            await Write(async writer =>
            {
                await writer.Write(new ModeCommand { Command = 5 });
            });
            byte[] data = null;
            await Read(async reader =>
            {
                data = (await reader.Read<RawData>())?.Data;
            });
            return data;
        }
    }
}
