using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Lucky.Home.Sinks.DigitalInputArraySink;

namespace Lucky.Home.Devices
{
    [Device("Digital Input Event Logger")]
    [RequiresArray(typeof(DigitalInputArraySink))]
    public class InputLoggerDevice : DeviceBase
    {
        private readonly List<SubSink> _inputSinks = new List<SubSink>();
        private static int s_count = 0;
        private int _instanceId;

        public InputLoggerDevice()
        {
            _instanceId = s_count++;
        }

        protected override void OnSinkChanged(SubSink removed, SubSink added)
        {
            base.OnSinkChanged(removed, added);

            if (removed != null)
            {
                ((DigitalInputArraySink)removed.Sink).EventReceived -= HandleEvent;
                _inputSinks.Remove(removed);
            }
            else if (added != null)
            {
                ((DigitalInputArraySink)added.Sink).EventReceived += HandleEvent;
                _inputSinks.Add(added);
            }
        }

        protected override Task OnTerminate()
        {
            foreach (var input in _inputSinks)
            {
                (input.Sink as DigitalInputArraySink).EventReceived -= HandleEvent;
            }
            return base.OnTerminate();
        }

        private void HandleEvent(object sender, EventReceivedEventArgs e)
        {   
            if (_inputSinks.Any(s => s.SubIndex == e.SubIndex && sender == s.Sink))
            {
                Console.WriteLine("[{3}]: Input {0} changed at {1}: {2}", e.SubIndex, e.Timestamp, e.State, _instanceId);
            }
        }
    }
}