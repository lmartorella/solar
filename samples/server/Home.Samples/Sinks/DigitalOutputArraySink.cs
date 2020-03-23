using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Lucky.Home.Serialization;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive output array
    /// </summary>
    [SinkId("DOAR")]
    internal class DigitalOutputArraySink : SinkBase
    {
        private bool[] _status;

        public DigitalOutputArraySink()
        {
            Status = new bool[0];
        }

        private class ReadStatusResponse
        {
            public ushort SwitchCount;
        }

        private class WriterStatusMessage : ISerializable
        {
            private byte[] _msg;

            public WriterStatusMessage()
            {

            }

            public WriterStatusMessage(int bitCount, byte[] data)
            {
                _msg = new byte[data.Length + 1];
                _msg[0] = (byte)bitCount;
                Array.Copy(data, 0, _msg, 1, data.Length);
            }

            public Task Deserialize(Func<int, Task<byte[]>> feeder)
            {
                throw new NotImplementedException();
            }

            public byte[] Serialize()
            {
                return _msg;
            }
        }

        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            await Read(async reader =>
            {
                var resp = await reader.Read<ReadStatusResponse>();
                SubCount = resp.SwitchCount;
                _status = new bool[SubCount];
            });

            // Align ext data
            await UpdateValues(_status);
        }

        public bool[] Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                UpdateValues(value);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private async Task UpdateValues(bool[] value)
        {
            if (SubCount != value.Length)
            {
                throw new ArgumentException("Array of values should match sub count");
            }
            if (IsOnline)
            {
                await Write(async writer =>
                {
                    var data = new byte[(SubCount - 1) / 8 + 1];
                    for (int i = 0; i < SubCount; i++)
                    {
                        data[i / 8] |= (byte)((value[i] ? 1 : 0) << (i % 8));
                    }
                    var msg = new WriterStatusMessage(SubCount, data);
                    await writer.Write(msg);
                });
            }
        }
    }
}