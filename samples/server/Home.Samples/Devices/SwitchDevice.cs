using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Switch device. All outputs are set to the XOR of all inputs, like
    /// classic electric wall switches.
    /// Needs at least one input (Digital Input Array) and one output (Digital Out Array)
    /// </summary>
    [Device("Xor Switch")]
    [RequiresArray(typeof(DigitalInputArraySink))]
    [RequiresArray(typeof(DigitalOutputArraySink))]
    public class SwitchDevice : DeviceBase
    {
        private readonly TimeSpan _period;

        private readonly List<SubSink> _inputs = new List<SubSink>();
        private readonly List<SubSink> _outputs = new List<SubSink>();

        private bool _lastStatus;

        public SwitchDevice()
        {
            _period = TimeSpan.FromMilliseconds(350);
        }

        protected override void OnSinkChanged(SubSink removed, SubSink added)
        {
            base.OnSinkChanged(removed, added);

            bool updated = false;
            if (removed != null)
            {
                var removedInput = removed.Sink as DigitalInputArraySink;
                if (removedInput != null)
                {
                    removedInput.StatusChanged -= HandleStatusChanged;
                    _inputs.Remove(removed);
                }
                else
                {
                    _outputs.Remove(removed);
                }
                updated = true;
            }

            if (added != null)
            {
                var addedInput = added.Sink as DigitalInputArraySink;
                if (addedInput != null)
                {
                    addedInput.StatusChanged += HandleStatusChanged;
                    // TODO
                    addedInput.PollPeriod = _period;
                    _inputs.Add(added);
                }
                else
                {
                    _outputs.Add(added);
                }
                updated = true;
            }

            if (updated)
            {
                UpdateOutput();
                HandleStatusChanged(null, null);
            }
        }

        private void HandleStatusChanged(object sender, EventArgs e)
        {
            if (_inputs.All(s => s.IsOnline))
            {
                Status = _inputs.Aggregate(false, (c, input) => c ^ ((input.Sink as DigitalInputArraySink).Status.Length > input.SubIndex && (input.Sink as DigitalInputArraySink).Status[input.SubIndex]));
            }
        }

        protected override Task OnTerminate()
        {
            foreach (var input in _inputs)
            {
                (input.Sink as DigitalInputArraySink).StatusChanged -= HandleStatusChanged;
            }
            return base.OnTerminate();
        }

        public bool Status
        {
            get 
            {
                return _lastStatus;
            }
            private set
            {
                if (_lastStatus != value)
                {
                    _lastStatus = value;
                    UpdateOutput();
                    if (StatusChanged != null)
                    {
                        StatusChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void UpdateOutput()
        {
            foreach (var output in _outputs)
            {
                if (output.SubIndex < (output.Sink as DigitalOutputArraySink).Status.Length)
                {
                    var state = (output.Sink as DigitalOutputArraySink).Status;
                    state[output.SubIndex] = Status;
                    (output.Sink as DigitalOutputArraySink).Status = state;
                }
            }
        }

        public event EventHandler StatusChanged;
    }
}