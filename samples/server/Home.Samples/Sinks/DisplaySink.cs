using System.Text;
using Lucky.Home.Serialization;
using System.Threading.Tasks;

#pragma warning disable CS0649 

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Simple line display protocol
    /// </summary>
    /// <remarks>
    /// Protocol: WRITE: raw ASCII data, no ending zero
    /// </remarks>
    [SinkId("LINE")]
    class DisplaySink : SinkBase
    {
        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            await Init();
        }

        private Task Init()
        { 
            return Read(async reader =>
            {
                var metadata = await reader.Read<ReadCapMessageResponse>();
                if (metadata != null)
                {
                    LineCount = metadata.LineCount;
                    CharCount = metadata.CharCount;
                }
                else
                {
                    await Task.Delay(1000);
                    await Init();
                }
            });
        }

        private class ReadCapMessageResponse
        {
            public short LineCount;
            public short CharCount;
        }

        private class LineMessage
        {
            [SerializeAsDynArray]
            public byte[] Data;
        }

        public Task Write(string line)
        {
            return Write(async writer =>
            {
                await writer.Write(new LineMessage { Data = Encoding.ASCII.GetBytes(line)});
            });
        }

        public int LineCount { get; private set; }
        public int CharCount { get; private set; }
    }
}
