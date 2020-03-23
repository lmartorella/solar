using Lucky.Home.Sinks;
using System;
using System.Threading.Tasks;
using static Lucky.Home.Sinks.DigitalInputArraySink;

namespace Lucky.Home.Devices
{
    [Device("Digital Input Event Logger")]
    [Requires(typeof(DigitalInputArraySink))]
    public class DigitalInputLoggerDevice : DeviceBase
    {
        private DigitalInputArraySink _inputSink;
        private SubSink _inputSubSink;
        private static int s_count = 0;
        private int _instanceId;

        public DigitalInputLoggerDevice()
        {
            _instanceId = s_count++;
        }

        protected override void OnSinkChanged(SubSink removed, SubSink added)
        {
            base.OnSinkChanged(removed, added);

            if (removed != null)
            {
                if (_inputSubSink != removed)
                {
                    throw new InvalidOperationException("Invalid sink removed");
                }
                _inputSink.EventReceived -= HandleEvent;
                _inputSink = null;
                _inputSubSink = null;
            }
            else if (added != null)
            {
                if (_inputSink != null)
                {
                    throw new InvalidOperationException("Multiple digital input sinks not supported for this device");
                }
                _inputSubSink = added;
                _inputSink = (DigitalInputArraySink)added.Sink;
                _inputSink.EventReceived += HandleEvent;

                // Sync status
                _inputSink.Initialized.ContinueWith(task =>
                {
                    var state = _inputSink.GetStatus(added.SubIndex);
                    Console.WriteLine("[{2}]: Input {0} started: {1}", _inputSubSink.SubIndex, state, _instanceId);
                });
            }
        }

        protected override Task OnTerminate()
        {
            if (_inputSink != null)
            {
                _inputSink.EventReceived -= HandleEvent;
            }
            return base.OnTerminate();
        }

        private void HandleEvent(object sender, EventReceivedEventArgs e)
        {
            if (e.SubIndex == _inputSubSink.SubIndex)
            {
                Console.WriteLine("[{3}]: Input {0} changed at {1}: {2}", e.SubIndex, e.Timestamp, e.State, _instanceId);
            }
        }
    }
}