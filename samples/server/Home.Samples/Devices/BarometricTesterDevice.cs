using Lucky.Home.Sinks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Tester device for Bosch BPM180 sensor and displays data on stdout
    /// </summary>
    [Device("Barometric Tester")]
    [Requires(typeof(BarometricSink))]
    class BarometricTesterDevice : DeviceBase
    {
        private Timer _timer;

        private class CalibrationData
        {
            private readonly short ac1;
            private readonly short ac2;
            private readonly short ac3;
            private readonly ushort ac4;
            private readonly ushort ac5;
            private readonly ushort ac6;
            private readonly short b1;
            private readonly short b2;
            private readonly short mb;
            private readonly short mc;
            private readonly short md;

            public CalibrationData(byte[] data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    for (int i = 0; i < data.Length; i += 2)
                    {
                        byte x = data[i];
                        data[i] = data[i + 1];
                        data[i + 1] = x;
                    }
                }
                ac1 = BitConverter.ToInt16(data, 0);
                ac2 = BitConverter.ToInt16(data, 2);
                ac3 = BitConverter.ToInt16(data, 4);
                ac4 = BitConverter.ToUInt16(data, 6);
                ac5 = BitConverter.ToUInt16(data, 8);
                ac6 = BitConverter.ToUInt16(data, 10);
                b1 = BitConverter.ToInt16(data, 12);
                b2 = BitConverter.ToInt16(data, 14);
                mb = BitConverter.ToInt16(data, 16);
                mc = BitConverter.ToInt16(data, 18);
                md = BitConverter.ToInt16(data, 20);
            }

            public void DecodeData(byte[] data, out float temperature, out float pressure)
            {
                const int oss = 3;

                // Uncompensate temp
                uint s_ut = ((uint)(data[0]) << 8) | (uint)data[1];
                // Uncompensate pressure
                uint s_up = (((uint)data[2] << 16) |
                       ((uint)data[3] << 8) |
                        (uint)data[4]) >> (8 - oss);

                int b5;
                {
                    // Calc true temp
                    int x1 = (((int)s_ut - (int)ac6) * (int)ac5) >> 15;
                    if (x1 == 0 && md == 0)
                    {
                        throw new ArgumentException("B.DIV");
                    }
                    int x2 = ((int)mc << 11) / (x1 + md);
                    b5 = x1 + x2;
                    short t = (short)((b5 + 8) >> 4);  // Temp in 0.1C
                    temperature = t / 10.0f;
                }
                {
                    int b6 = b5 - 4000;
                    int x1 = ((int)b2 * ((b6 * b6) >> 12)) >> 11;
                    int x2 = ((int)ac2 * b6) >> 11;
                    int x3 = x1 + x2;
                    int b3 = ((((int)ac1 * 4 + x3) << oss) + 2) >> 2;
                    x1 = ((int)ac3 * b6) >> 13;
                    x2 = ((int)b1 * ((b6 * b6) >> 12)) >> 16;
                    x3 = ((x1 + x2) + 2) >> 2;
                    uint b4 = (uint)((int)ac4 * (uint)(x3 + 32768)) >> 15;
                    uint b7 = ((uint)(s_up - b3) * (50000u >> oss));
                    if (b4 == 0)
                    {
                        throw new ArgumentException("B.DIV2");
                    }
                    int p;
                    if (b7 < 0x80000000)
                    {
                        p = (int)((b7 << 1) / b4);
                    }
                    else
                    {
                        p = (int)(b7 / b4) << 1;
                    }
                    x1 = (((p >> 8) * (p >> 8)) * 3038) >> 16;
                    x2 = (p * -7357) >> 16;
                    p += (x1 + x2 + 3791) >> 4; // in Pascal
                    pressure = p / 100.0f; // in hPa
                }

            }
        }

        private CalibrationData _calibData;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _timer = new Timer(async o =>
            {
                if (OnlineStatus == OnlineStatus.Online)
                {
                    var sink = Sinks.OfType<BarometricSink>().FirstOrDefault();
                    if (sink != null)
                    {
                        if (_calibData == null)
                        {
                            byte[] data = await sink.ReadCalibrationData();
                            if (data != null && data.Length == 22)
                            {
                                WriteData(sink, data);
                                _calibData = new CalibrationData(data);
                            }
                        }
                        else
                        {
                            // Read data
                            byte[] data = await sink.ReadUncompensatedData();
                            if (data != null && data.Length == 5)
                            {
                                float pressure;
                                float temperature;
                                _calibData.DecodeData(data, out temperature, out pressure);
                                Console.WriteLine("{0}: {1}'C, {2}hPa", sink, temperature, pressure);
                            }
                        }
                    }
                }
            }, null, 0, 1500);
        }

        private void WriteData(BarometricSink sink, byte[] data)
        {
            Console.WriteLine("{0}: {1}", sink.ToString(), string.Join(" ", data.Select(b => b.ToString("x2"))));
        }

        protected override Task OnTerminate()
        {
            _timer.Dispose();
            return base.OnTerminate();
        }
    }
}
