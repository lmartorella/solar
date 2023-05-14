using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class GardenSetImmediateRpcRequest : RpcRequest
    {
        [DataMember(Name = "immediate")]
        public ImmediateZone ImmediateZone { get; set; }
    }

    [DataContract]
    public class GardenSetConfigRpcRequest : RpcRequest
    {
        [DataMember(Name = "config")]
        public Configuration Configuration { get; set; }
    }

    [DataContract]
    public class ImmediateZone
    {
        [DataMember(Name = "zones")]
        public int[] Zones { get; set; }

        [DataMember(Name = "time")]
        public int Time { get; set; }

        public override string ToString()
        {
            return ToString("");
        }

        public string ToString(string format, string[] zoneNames = null)
        {
            switch (format) 
            {
                case "f":
                    return string.Format(Resources.immediateToString, string.Join(", ", Zones.Select(i => zoneNames[i])), Time);
                default:
                    return "Zones: " + string.Join(",", Zones) + " [Time: " + Time + "]";
            }
        }
    }

    [DataContract]
    public class GardenStatusRpcResponse : RpcResponse
    {
        /// <summary>
        /// Configuration
        /// </summary>
        [DataMember(Name = "config")]
        public Configuration Configuration { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "status")]
        public OnlineStatus Status{ get; set; }

        /// <summary>
        /// Is the program running right now?
        /// </summary>
        [DataMember(Name = "isRunning")]
        public bool IsRunning { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "flowData")]
        public FlowData FlowData { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "nextCycles")]
        public NextCycle[] NextCycles { get; set; }
    }
}
