using Lucky.Home.Model;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;

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

        [DataContract]
        public class ZoneSetName
        {
            [DataMember(Name = "name")]
            public string Name;

            [DataMember(Name = "zones")]
            public int[] Zones;
        }

        [DataMember(Name = "zoneSets")]
        public ZoneSetName[] ZoneSetNames { get; set; }

        internal string GetCycleName(GardenCycle cycle)
        {
            return GetCycleName(cycle.Zones);
        }

        internal string GetCycleName(ZoneTime program)
        {
            return GetCycleName(program.Zones);
        }

        private string GetCycleName(int[] zones)
        {
            if (zones.Length > 1)
            {
                // Find combination
                var matching = ZoneSetNames.FirstOrDefault(t => t.Zones.SequenceEqual(zones));
                if (matching != null)
                {
                    return matching.Name;
                }
                else
                {
                    return string.Join(", ", zones.Select(i => ZoneNames[i]));
                }
            }
            else if (zones.Length == 1)
            {
                return ZoneNames[zones[0]];
            }
            else
            {
                return "<none>";
            }
        }
    }
}

