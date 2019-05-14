using System;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive/poll based switch array
    /// </summary>
    [SinkId("DIAR")]
    internal class DigitalInputArraySink : SinkBase
    {
        public TimeSpan PollPeriod = TimeSpan.FromSeconds(5);
        private bool[] _status = new bool[0];

        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            byte[] lastData = null;
            await Read(async reader =>
            {
                var resp = await reader.Read<ReadStatusResponse>();
                SubCount = resp.SwitchesCount;
                lastData = resp.Data;
                var ret = new bool[SubCount];
                for (int i = 0; i < SubCount; i++)
                {
                    ret[i] = (lastData[i / 8] & (1 << (i % 8))) != 0;
                }
                Status = ret;
            });

            RunLoop(lastData);
        }

        private class ReadStatusResponse : ISerializable
        {
            /// <summary>
            /// Number of switches
            /// </summary>
            public int SwitchesCount; 

            /// <summary>
            /// Switch data in bytes 
            /// </summary>
            public byte[] Data;

            public async Task Deserialize(Func<int, Task<byte[]>> feeder)
            {
                byte[] size = await feeder(1);
                // Size is in bits
                SwitchesCount = size[0];
                int len = (SwitchesCount - 1) / 8 + 1;
                Data = await feeder(len);
            }

            public byte[] Serialize()
            {
                throw new NotImplementedException();
            }
        }

        public bool[] Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                SubCount = _status.Length;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void RunLoop(byte[] lastData)
        {
            while (true)
            {
                await Task.Delay(PollPeriod);

                bool[] ret = null;
                await Read(async reader =>
                {
                    var resp = await reader.Read<ReadStatusResponse>();
                    if (resp != null)
                    {
                        if (!resp.Data.SequenceEqual(lastData))
                        {
                            // Something changed
                            int swCount = Math.Min(resp.SwitchesCount, resp.Data.Length * 8);
                            ret = new bool[swCount];
                            for (int i = 0; i < swCount; i++)
                            {
                                ret[i] = (resp.Data[i / 8] & (1 << (i % 8))) != 0;
                            }
                            lastData = resp.Data;
                        }
                    }
                });

                // Outside the Read to avoid reenter of other sinks
                if (ret != null)
                {
                    Status = ret;
                }
            }
        }

        public event EventHandler StatusChanged;
    }
}