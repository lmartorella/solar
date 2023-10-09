using System.Runtime.Serialization;

namespace Lucky.Home.Solar
{
    [DataContract]
    public class SolarRpcResponse
    {
        /// <summary>
        /// Is the sink online?
        /// </summary>
        [DataMember(Name = "status")]
        public OnlineStatus Status{ get; set; }

        [DataMember(Name = "currentW")]
        public double CurrentW { get; set; }

        [DataMember(Name = "currentTs")]
        public string CurrentTs { get; set; }

        [DataMember(Name = "totalDayWh")]
        public double TotalDayWh { get; set; }

        [DataMember(Name = "totalKwh")]
        public double TotalKwh { get; set; }

        [DataMember(Name = "inverterState")]
        public string InverterState { get; set; }

        [DataMember(Name = "peakW")]
        public double PeakW { get; set; }

        [DataMember(Name = "peakWTs")]
        public string PeakWTs { get; set; }

        [DataMember(Name = "peakV")]
        public double PeakV { get; set; }

        [DataMember(Name = "peakVTs")]
        public string PeakVTs { get; set; }

        // Home usage power, to report Net Energy Metering
        [DataMember(Name = "gridV")]
        public double GridV { get; set; }
        [DataMember(Name = "usageA")]
        public double UsageA { get; set; }
    }
}
