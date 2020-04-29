using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Poll-based switch set. Not suitable for real-time event reaction. Best suitable for historical event logging.
    /// Polls the status of each digital port, and receives the historical event log about last changes, with high-resolution timestamp.
    /// </summary>
    [SinkId("DIAR")]
    internal class DigitalInputArraySink : SinkBase
    {
        /// <summary>
        /// Poll events every few seconds. No need for further precision since the history is stored in the remote device.
        /// </summary>
        private TimeSpan PollPeriod = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The sink state changed in a moment in time (past). 
        /// </summary>
        private class BulkChangeEvent
        {
            /// <summary>
            /// Event timestamp
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// All switches state
            /// </summary>
            public bool[] State;

            /// <summary>
            /// If the event is syntethic, raised since the cached data differs from the current sink data.
            /// </summary>
            public bool Recovered;
        }

        /// <summary>
        /// A single input (sub-sink) changed in a moment in time (past)
        /// </summary>
        public class EventReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// Event timestamp
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// Single switch state (sub-sink)
            /// </summary>
            public bool State;

            /// <summary>
            /// SubSink index
            /// </summary>
            public int SubIndex;
        }

        public bool GetStatus(int subIndex)
        {
            return subIndex < _lastState.Length ? _lastState[subIndex] : false;
        }

        private bool[] _lastState;

        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            await Read(async reader =>
            {
                var resp = await reader.Read<ReadStatusResponse>();
                _lastState = resp.CurrentState.State;
                SubCount = _lastState.Length;
            });

            // Don't await this
            RunLoop();
        }

        private class ReadStatusResponse : ISerializable
        {
            /// <summary>
            /// Current state and time
            /// </summary>
            public BulkChangeEvent CurrentState;

            /// <summary>
            /// Previous observed changed events
            /// </summary>
            public BulkChangeEvent[] Events;

            private ulong FromTicks(byte[] ticks)
            {
                switch (ticks.Length)
                {
                    case 1:
                        return ticks[0];
                    case 2:
                        return BitConverter.ToUInt16(ticks, 0);
                    case 4:
                        return BitConverter.ToUInt32(ticks, 0);
                    case 8:
                        return BitConverter.ToUInt64(ticks, 0);
                    default:
                        throw new InvalidOperationException("Tick size not supported: " + ticks.Length);
                }
            }

            private ulong DiffTicks(ulong t2, ulong t1, int size)
            {
                switch (size)
                {
                    case 1:
                        return (ulong)((byte)t2 - (byte)t1);
                    case 2:
                        ushort d = (ushort)((ushort)t2 - (ushort)t1);
                        return d;
                    case 4:
                        return ((uint)t2 - (uint)t1);
                    case 8:
                        return t2 - t1;
                    default:
                        throw new InvalidOperationException("Tick size not supported: " + size);
                }
            }

            private bool[] FromBits(byte[] data, int bitCount)
            {
                var ret = new bool[bitCount];
                for (int i = 0; i < bitCount; i++)
                {
                    ret[i] = (data[i / 8] & (1 << (i % 8))) != 0;
                }
                return ret;
            }

            public async Task Deserialize(Func<int, Task<byte[]>> feeder)
            {
                // Read tick size and bit count
                byte[] size = await feeder(1);
                // Size is in bits
                int bitCount = size[0] >> 4;
                int tickSize = size[0] & 0xf;

                int byteLen = (bitCount - 1) / 8 + 1;
                var currentState = await feeder(byteLen);

                // Now read ticks/seconds
                ulong ticksPerSecond = FromTicks(await feeder(tickSize));
                // Align hw timestamp with the receiver
                ulong nowT = FromTicks(await feeder(tickSize));
                DateTime now = DateTime.Now;

                CurrentState = new BulkChangeEvent { State = FromBits(currentState, bitCount), Timestamp = now };

                int l = (await feeder(1))[0];
                Events = new BulkChangeEvent[l];
                for (int i = 0; i < l; i++)
                {
                    var time = FromTicks(await feeder(tickSize));
                    var data = await feeder(byteLen);
                    ulong delta = DiffTicks(nowT, time, tickSize);
                    DateTime timestamp = now - TimeSpan.FromSeconds((double)delta / ticksPerSecond);
                    Events[i] = new BulkChangeEvent { State = FromBits(data, bitCount), Timestamp = timestamp };
                }
            }

            public byte[] Serialize()
            {
                throw new NotImplementedException();
            }
        }

        private async void RunLoop()
        {
            while (true)
            {
                await Task.Delay(PollPeriod);
                if (!IsOnline) {
                    continue;
                }

                try
                {
                    var events = new List<EventReceivedEventArgs>();
                    await Read(async reader =>
                    {
                        var resp = await reader.Read<ReadStatusResponse>();
                        if (resp != null)
                        {
                            // It is possible to receive no events but data changed compared to the cached copy... In that case creates a changed event
                            // with the current time.
                            if (resp.Events.Length == 0 && !resp.CurrentState.State.SequenceEqual(_lastState))
                            {
                                resp.CurrentState.Recovered = true;
                                resp.Events = new[] { resp.CurrentState };
                            }

                            // Now process events 
                            foreach (var change in resp.Events)
                            {
                                // Calc changed bits and prepare single events
                                for (int i = Math.Min(_lastState.Length, change.State.Length) - 1; i >= 0; i--)
                                {
                                    if (_lastState[i] != change.State[i])
                                    {
                                        events.Add(new EventReceivedEventArgs { State = change.State[i], SubIndex = i, Timestamp = change.Timestamp });
                                    }
                                }
                                _lastState = change.State;
                            }

                            // Sync current state
                            _lastState = resp.CurrentState.State;
                        }
                    });

                    // Outside the Read to avoid reenter of other sinks
                    var raise = EventReceived;
                    if (raise != null && events.Count > 0)
                    {
                        foreach (var evt in events)
                        {
                            raise(this, evt);
                        }
                    }
                }
                catch (Exception exc)
                {
                    Logger.Exception(exc);
                }
            }
        }

        /// <summary>
        /// Event raised when one sub changed (with timestamp)
        /// </summary>
        public event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}