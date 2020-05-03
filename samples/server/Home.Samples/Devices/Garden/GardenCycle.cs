using Lucky.Home.Model;
using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class GardenCycle : TimeProgram<GardenCycle>.Cycle
    {
        [DataMember(Name = "minutes")]
        public int Minutes;

        [DataMember(Name = "zones")]
        public int[] Zones;

        [IgnoreDataMember]
        internal TimeSpan NomimalDuration
        {
            get
            {
                return TimeSpan.FromMinutes(Minutes);
            }
        }

        internal ZoneTime ToZoneTime()
        {
            return new ZoneTime { Minutes = Minutes, Zones = Zones };
        }
    }
}

