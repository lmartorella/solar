using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class GardenWebRequest : WebRequest
    {
        [DataMember(Name = "immediate")]
        public ImmediateZone[] ImmediateZones { get; set; }
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
            return "Zones: " + string.Join(",", Zones) + " [Time: " + Time + "]";
        }
    }

    [DataContract]
    public class GardenWebResponse : WebResponse
    {
        /// <summary>
        /// Configuration
        /// </summary>
        [DataMember(Name = "config")]
        public Configuration Configuration { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "online")]
        public bool Online { get; set; }

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
