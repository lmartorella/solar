using Lucky.Home.Serialization;
using Lucky.Home.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    class TempMessage
    {
        [SerializeAsFixedArray(6)]
        public byte[] Data;
    }

    enum TemperatureSinkErrCode : byte
    {
        ERR_OK = 1,
        ERR_MISSING_LEADING_HIGH = 2,  // Infinite level 0 after engage
        ERR_MISSING_LEADING_LOW = 3,  // Infinite level 1 after engage
        ERR_AT_BYTE = 0x10,         // Err at byte 0x10 + N
    }

    enum TemperatureSinkStatus
    {
        Ok = 0,
        HwErrMissingLeadingHigh = TemperatureSinkErrCode.ERR_MISSING_LEADING_HIGH,
        HwErrMissingLeadingLow = TemperatureSinkErrCode.ERR_MISSING_LEADING_LOW,
        HwErrByte0 = TemperatureSinkErrCode.ERR_AT_BYTE + 0,
        HwErrByte1 = TemperatureSinkErrCode.ERR_AT_BYTE + 1,
        HwErrByte2 = TemperatureSinkErrCode.ERR_AT_BYTE + 2,
        HwErrByte3 = TemperatureSinkErrCode.ERR_AT_BYTE + 3,
        HwErrByte4 = TemperatureSinkErrCode.ERR_AT_BYTE + 4,
        ChecksumError = 100,
        DataError = 101
    }

    class TemperatureReading
    {
        public TemperatureSinkStatus SinkStatus;
        public short Humidity;
        public short Temperature;
    }

    /// <summary>
    /// Read data from a DHT11 temperature sensor
    /// </summary>
    [SinkId("TEMP")]
    class TemperatureSink : SinkBase
    {
        public async Task<TemperatureReading> Read()
        {
            byte[] data = null;
            await Read(async reader =>
            {
                data = (await reader.Read<TempMessage>()).Data;
            });

            if (data == null || data.Length != 6)
            {
                Logger.Error("ProtocolError", "Len", data != null ? data.Length : -1);
                return new TemperatureReading { SinkStatus = TemperatureSinkStatus.DataError };
            }
            if (data[0] != (byte)TemperatureSinkErrCode.ERR_OK)
            {
                Logger.Error("BeanErrorReadingSensor");
                return new TemperatureReading { SinkStatus = (TemperatureSinkStatus)data[0] };
            }

            // Calc checksum
            if ((byte)data.Skip(1).Take(4).Sum(b => b) != data[5])
            {
                Logger.Error("ChecksumError");
                return new TemperatureReading { SinkStatus = TemperatureSinkStatus.ChecksumError };
            }

            short u1 = BitConverter.ToInt16(data, 1);
            short u2 = BitConverter.ToInt16(data, 3);

            return new TemperatureReading { SinkStatus = TemperatureSinkStatus.Ok, Humidity = u1, Temperature = u2 };
        }
    }
}
