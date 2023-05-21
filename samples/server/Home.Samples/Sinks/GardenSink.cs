using Lucky.Home.Serialization;
using Lucky.Home.Services;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Interface for garden programmer sink
    /// </summary>
    [SinkId("GARD")]
    public class GardenSink : SinkBase
    {
        public enum DeviceState : byte
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

        public class ReadStatusMessageResponse
        {
            public DeviceState State;

            [SerializeAsDynArray]
            public ImmediateZoneTime[] ZoneTimes;
        }

        [DataContract]
        public class ImmediateZoneTime
        {
            [DataMember(Name = "minutes")]
            public int Minutes;

            [DataMember(Name = "zoneMask")]
            public int ZoneMask;
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
        private MqttService mqqtService;

        [DataContract]
        public class TimerState
        {
            [DataMember(Name = "isAvailable")]
            public bool IsAvailable;

            [DataMember(Name = "zoneRemTimes")]
            public ImmediateZoneTime[] ZoneRemTimes;
        }

        [DataContract]
        public class Program
        {
            [DataMember(Name = "times")]
            public ImmediateZoneTime[] Times;
        }

        public GardenSink()
        {
            mqqtService = Manager.GetService<MqttService>();
            _ = Subscribe();
        }

        private async Task Subscribe()
        {
            await mqqtService.SubscribeRawRpc("garden_timer_0/reset", req =>
            {
                ResetNode();
                return Task.FromResult(null as byte[]);
            });
            await mqqtService.SubscribeJsonRpc<RpcVoid, TimerState>("garden_timer_0/state", async req =>
            {
                return await Read(false);
            });
            await mqqtService.SubscribeJsonRpc<Program, RpcVoid>("garden_timer_0/program", async req =>
            {
                await WriteProgram(req.Times);
                return new RpcVoid();
            });
            await mqqtService.SubscribeRawRpc("garden_timer_0/setFlow", async req =>
            {
                await UpdateFlowData(int.Parse(Encoding.UTF8.GetString(req)));
                return null;
            });
        }

        private async Task<TimerState> Read(bool log)
        {
            TimerState state = null;
            await Read(async reader =>
            {
                var md = await reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    if (log)
                    {
                        Logger.Log("GardenMd", "State", md.State, "Times", string.Join(", ", md.ZoneTimes.Select(t => t.Minutes.ToString())));
                    }
                    state = new TimerState { IsAvailable = md.State == DeviceState.Off, ZoneRemTimes = md.ZoneTimes };
                }
                else
                {
                    Logger.Log("GardenMd NO DATA");
                }
            }, 3000);
            return state;
        }

        private async Task WriteProgram(ImmediateZoneTime[] zoneTimes)
        {
            if (zoneTimes.All(z => z.Minutes <= 0))
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

        private async Task UpdateFlowData(int flow)
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
