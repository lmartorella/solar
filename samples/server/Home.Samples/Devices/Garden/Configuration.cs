using Lucky.Home.Model;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    /// <summary>
    /// JSON for configuration serialization
    /// </summary>
    [DataContract]
    public class Configuration
    {
        [DataMember(Name = "program")]
        public TimeProgram<GardenCycle>.ProgramData Program { get; set; }

        [DataMember(Name = "zones")]
        public string[] ZoneNames { get; set; }
    }
}

