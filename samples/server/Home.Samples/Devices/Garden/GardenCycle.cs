using Lucky.Home.Model;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class GardenCycle : TimeProgram<GardenCycle>.Cycle
    {
        /// <summary>
        /// 1 up to 4 "cycles" zone program. Extended: multiple concurrent zone at the same time
        /// </summary>
        [DataMember(Name = "zoneTimes")]
        public ZoneTime[] ZoneTimes;

        [IgnoreDataMember]
        internal TimeSpan NomimalDuration
        {
            get
            {
                return TimeSpan.FromMinutes(ZoneTimes.Sum(zt => zt.Minutes));
            }
        }
    }
}

