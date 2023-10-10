using Lucky.Db;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// CSV for tick-by-tick power data
    /// </summary>
    public class PowerData : TimeSample
    {
        [Csv("0")]
        public double PowerW;
        [Csv("0")]
        public double TotalEnergyKWh;
        [Csv(InverterStates.Normal)]
        public string InverterState;
        [Csv("0")]
        public double EnergyTodayWh;
        [Csv("0.00")]
        public double GridCurrentA;
        [Csv("0.0")]
        public double GridVoltageV;
        [Csv("0.00")]
        public double GridFrequencyHz;
        [Csv("0.00")]
        public double String1CurrentA;
        [Csv("0.00")]
        public double String1VoltageV;
        [Csv("0.00")]
        public double String2CurrentA;
        [Csv("0.00")]
        public double String2VoltageV;
        // Home usage current, to calculate Net Energy Metering
        [Csv("0.00")]
        public double HomeUsageCurrentA;
    }
}
