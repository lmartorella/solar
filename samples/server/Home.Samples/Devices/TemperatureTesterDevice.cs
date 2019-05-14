using System.Threading;
using Lucky.Home.Services;
using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Test a temperature sink and displays data on stdout
    /// </summary>
    [Device("Temperature Tester")]
    [Requires(typeof(TemperatureSink))]
    public class TemperatureTesterDevice : DeviceBase
    {
        private Timer _timer;

        public TemperatureTesterDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    TemperatureReading reading = await ((TemperatureSink)Sinks[0]).Read();
                    if (reading.SinkStatus == TemperatureSinkStatus.Ok)
                    {
                        Logger.Log("Reading", "RH%", ToDec(reading.Humidity), "T(C)", ToDec(reading.Temperature));
                    }
                }

            }, null, 0, 2000);
        }

        private static float ToDec(short bigEndian)
        {
            sbyte decPart = (sbyte)(bigEndian >> 8);
            byte intPart = (byte)(bigEndian & 0xff);
            return intPart + decPart / 256.0f;
        }
    }
}
