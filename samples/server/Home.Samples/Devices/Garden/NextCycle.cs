using Lucky.Home.Model;
using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    [DataContract]
    public class NextCycle
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "scheduledTime")]
        public string ScheduledTimeStr { get { return ScheduledTime.ToIso(); } set { ScheduledTime = value.FromIso(); } }
        [IgnoreDataMember]
        public DateTime? ScheduledTime { get; set; }
    }
}

