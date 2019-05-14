using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Uses a display line to display current time
    /// </summary>
    [Device("Clock")]
    [RequiresArray(typeof(DisplaySink))]
    public class ClockDevice : DeviceBase
    {
        public ClockDevice()
        {
            StartLoop();
        }

        private async Task StartLoop()
        {
            while (!IsDisposed)
            {
                if (IsFullOnline)
                {
                    var str = DateTime.Now.ToString("HH:mm:ss");
                    foreach (var sink in Sinks.OfType<DisplaySink>())
                    {
                        await sink.Write(str);
                    }
                }
                await Task.Delay(500);
            }
        }
    }
}
