using Lucky.Home.Services;

namespace Lucky.Home.Solar
{
    public class SolarConfiguration
    {
        public string InverterHostName = "sofar";
        public string AmmeterHostName = "ammeter";
        public int InverterStationId = 1;
        public int AmmeterStationId = 1;
    }

    internal class SolarConfigurationService : ServiceBaseWithData<SolarConfiguration>
    {
        public SolarConfigurationService() 
            :base(true, false)
        {
        }
    }
}
