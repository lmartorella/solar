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

        internal NextCycle(ImmediateProgram q)
        {
            Name = q.Name;
        }

        public NextCycle(GardenCycle cycle, DateTime scheduleddTime)
        {
            Name = cycle.Name;
            ScheduledTime = scheduleddTime;
            Suspended = cycle.Suspended;
        }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "suspended")]
        public bool Suspended { get; set; }

        [DataMember(Name = "scheduledTime")]
        public string ScheduledTimeStr { get { return ScheduledTime.ToIso(); } set { ScheduledTime = value.FromIso(); } }
        [IgnoreDataMember]
        public DateTime? ScheduledTime { get; set; }
    }
}

