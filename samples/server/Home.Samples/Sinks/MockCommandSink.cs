using Lucky.Home.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    [SinkId("TCMD")]
    class MockCommandSink : SinkBase
    {
        public async Task<string> ReadCommand()
        {
            string command = null;
            await Read(async reader =>
            {
                command = (await reader.Read<DynamicString>())?.Str;
            });
            return command;
        }

        public Task WriteResponse(string response)
        {
            return Write(writer =>
            {
                return writer.Write(new DynamicString() { Str = response });
            });
        }
    }
}
