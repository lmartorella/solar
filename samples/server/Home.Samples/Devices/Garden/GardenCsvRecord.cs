using Lucky.Db;
using System;

namespace Lucky.Home.Devices.Garden
{
    class GardenCsvRecord
    {
        [Csv("yyyy-MM-dd")]
        public DateTime Date;

        /// <summary>
        /// Time of day of the first sample with power > 0
        /// </summary>
        [Csv("hh\\:mm\\:ss")]
        public TimeSpan Time;

        [Csv]
        public string Cycle;

        [Csv]
        public string Zones;

        /// <summary>
        /// 0: stoped (final). 1 = just started. 2 = flowing
        /// </summary>
        [Csv]
        public int State;

        /// <summary>
        /// In liter/seconds
        /// </summary>
        [Csv("0.0")]
        public double FlowLMin;

        /// <summary>
        /// Liter used for cycle
        /// </summary>
        [Csv("0.0")]
        public double QtyL;

        /// <summary>
        /// Total MC
        /// </summary>
        [Csv("0.000")]
        public double TotalQtyMc;
    }
}

