using Lucky.Home.Sinks;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    [Device("Serial Echo")]
    [Requires(typeof(HalfDuplexLineSink))]
    public class EchoLineTesterDevice : DeviceBase
    {
        private Timer _timer;
        private string _lastMessage = "0123456789abcdef0123456789abcdef 123456789abcdef0123456789abcdef$"; // = 65 chars
        //private string _lastMessage = "abc123$"; 

        public EchoLineTesterDevice()
        {
            _timer = new Timer(o =>
            {
                if (IsFullOnline)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Check();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }, null, 0, 3000);
        }

        private async Task Check()
        {
            var v = Encoding.ASCII.GetBytes(_lastMessage);
            Console.WriteLine("ECHO: TX -> {0} bytes", v.Length);
            var ret = await Sinks.OfType<HalfDuplexLineSink>().First().SendReceive(v, true, false, "check");
            if (ret.Item1 != null)
            {
                Console.WriteLine("ECHO: RX  <- {0} ({1} bytes)", Encoding.ASCII.GetString(ret.Item1), ret.Item1.Length);
                //_lastMessage = _lastMessage.ToUpper();
            }
        }
    }
}
