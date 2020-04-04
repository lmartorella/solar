using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class ZoneTime
    {
        [DataMember(Name = "minutes")]
        public int Minutes;

        [DataMember(Name = "zones")]
        public int[] Zones;
    }
}

