using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class GardenWebResponse : WebResponse
    {
        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Result/error
        /// </summary>
        [DataMember(Name = "error")]
        public string Error { get; set; }

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

