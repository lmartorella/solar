using Lucky.Home.Sinks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Tester device for Bosch BPM180 sensor and displays data on stdout
    /// </summary>
    [Device("Ammter Tester")]
    [Requires(typeof(AnalogIntegratorSink))]
    [Requires(typeof(DisplaySink))]
    class AmmeterTesterDevice : DeviceBase
    {
        private Timer _timer;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _timer = new Timer(async o =>
            {
                var integrator = GetFirstOnlineSink<AnalogIntegratorSink>();
                if (integrator != null)
                {
                    var reading = await integrator.ReadData();
                    var text = reading != null ? string.Format("{0:0.00}A", reading) : "N/A";
                    var display = GetFirstOnlineSink<DisplaySink>();
                    if (display != null)
                    {
                        await display.Write(text);
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }
                }
            }, null, 0, 1500);
        }

        protected override Task OnTerminate()
        {
            _timer.Dispose();
            return base.OnTerminate();
        }
    }
}
