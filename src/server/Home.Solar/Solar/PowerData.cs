using Lucky.Db;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// CSV for tick-by-tick power data
    /// </summary>
    public class PowerData : TimeSample
    {
        [Csv("0")]
        public double PowerW { get; set; }
        [Csv("0")]
        public double TotalEnergyKWh { get; set; }
        [Csv(InverterStates.Normal)]
        public string InverterState { get; set; }
        [Csv("0")]
        public double EnergyTodayWh { get; set; }
        [Csv("0.00")]
        public double GridCurrentA { get; set; }
        [Csv("0.0")]
        public double GridVoltageV { get; set; }
        [Csv("0.00")]
        public double GridFrequencyHz { get; set; }
        [Csv("0.00")]
        public double String1CurrentA { get; set; }
        [Csv("0.00")]
        public double String1VoltageV { get; set; }
        [Csv("0.00")]
        public double String2CurrentA { get; set; }
        [Csv("0.00")]
        public double String2VoltageV { get; set; }
        // Home usage current, to calculate Net Energy Metering
        [Csv("0.00")]
        public double HomeUsageCurrentA { get; set; }
    }
}
