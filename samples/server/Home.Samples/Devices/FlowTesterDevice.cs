using Lucky.Home.Sinks;
using System;
using System.Linq;
using System.Threading;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Test device for water flow sinks
    /// </summary>
    [Device("Flow Tester")]
    [Requires(typeof(FlowSink))]
    class FlowTesterDevice : DeviceBase
    {
        private Timer _timer;

        public FlowTesterDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    var data = await FlowSink.ReadData(5.5);
                    Console.WriteLine("Counter: {0}, Flow: {1}", data.TotalMc, data.FlowLMin);
                }
            }, null, 0, 1000);
        }

        private FlowSink FlowSink
        {
            get
            {
                return Sinks.OfType<FlowSink>().FirstOrDefault();
            }
        }
    }
}
