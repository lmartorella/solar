using Lucky.Home.Model;
using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class NextCycle
    {
        public NextCycle()
        { }

        public NextCycle(GardenCycle gardenCycle, Configuration configuration, DateTime? scheduledTime)
        {
            Name = configuration.GetCycleName(gardenCycle);
            ScheduledTime = scheduledTime;
            Suspended = gardenCycle.Suspended;
        }

        public NextCycle(ZoneTime zoneTime, Configuration configuration, bool running)
        {
            Name = configuration.GetCycleName(zoneTime);
            Suspended = false;
            Running = running;
        }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "running")]
        public bool Running { get; set; }

        [DataMember(Name = "suspended")]
        public bool Suspended { get; set; }

        [DataMember(Name = "scheduledTime")]
        public string ScheduledTimeStr { get { return ScheduledTime.ToIso(); } set { ScheduledTime = value.FromIso(); } }
        [IgnoreDataMember]
        public DateTime? ScheduledTime { get; set; }
    }
}

