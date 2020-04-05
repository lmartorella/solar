using Lucky.Home.Serialization;
using Lucky.Home.Services;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Interface for garden programmer sink
    /// </summary>
    [SinkId("GARD")]
    class GardenSink : SinkBase
    {
        private enum DeviceState : byte
        {
            Off = 0,
            // Immediate program mode
            ProgramImmediate,
            // Display flow level
            FlowCheck,
            // Looping a program (manual or automatic)
            InUse,
            // Timer used after new programming, while the display shows OK, to go back to imm state (2 seconds)
            WaitForImmediate
        }

        private class ReadStatusMessageResponse
        {
            public DeviceState State;

            [SerializeAsDynArray]
            public ImmediateZoneTime[] ZoneTimes;
        }

        public class ImmediateZoneTime
        {
            public byte Time;
            public byte ZoneMask;
        }

        private class WriteProgramMessageRequest
        {
            [SerializeAsDynArray]
            public ImmediateZoneTime[] ZoneTimes;
        }

        private class UpdateFlowMessageRequest
        {
            public short Code = -1;
            public short Flow;
        }

        private int _lastFlow = -1;

        public class TimerState
        {
            public bool IsAvailable;
            public ImmediateZoneTime[] ZoneRemTimes;
        }

        public async Task<TimerState> Read(bool log, int timeout = 3000)
        {
            TimerState state = null;
            await Read(async reader =>
            {
                var md = await reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    if (log)
                    {
                        Logger.Log("GardenMd", "State", md.State, "Times", string.Join(", ", md.ZoneTimes.Select(t => t.Time.ToString())));
                    }
                    state = new TimerState { IsAvailable = md.State == DeviceState.Off, ZoneRemTimes = md.ZoneTimes };
                }
                else
                {
                    Logger.Log("GardenMd NO DATA");
                }
            }, timeout);
            return state;
        }

        public async Task WriteProgram(ImmediateZoneTime[] zoneTimes)
        {
            if (zoneTimes.All(z => z.Time <= 0))
            {
                return;
            }

            await Write(async writer =>
            {
                await writer.Write(new WriteProgramMessageRequest
                {
                    ZoneTimes = zoneTimes
                });
            });
            // Log aloud new state
            await Read(true);
        }

        public async Task UpdateFlowData(int flow)
        {
            if (flow != _lastFlow)
            {
                await Write(async writer =>
                {
                    await writer.Write(new UpdateFlowMessageRequest
                    {
                        Flow = (short)flow
                    });
                });
                _lastFlow = flow;
            }
        }
    }
}
