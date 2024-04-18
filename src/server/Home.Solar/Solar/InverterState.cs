namespace Lucky.Home.Solar
{
    public enum OperatingState
    {
        Waiting = 0,
        Checking = 1,
        Normal = 2,
        Fault = 3,
        PermanentFault = 4,
        MaxKnownValue = 4,
        Unknown = -1
    }

    /// <summary>
    /// Known inverter states. Unknown state will be logged with original flags
    /// </summary>
    public class InverterState
    {
        public InverterState(OperatingState operatingState)
        {
            OperatingState = operatingState;
        }

        public InverterState(OperatingState operatingState, string faultBits)
        {
            OperatingState = operatingState;
            FaultBits = faultBits;
        }

        public OperatingState OperatingState { get; }
        public string FaultBits { get; }

        internal string ToCsv()
        {
            switch (OperatingState)
            {
                case OperatingState.Normal:
                    return "";
                default:
                    return OperatingState.ToString() + ":" + FaultBits;
            }
        }

        internal string ToUserInterface(Device.Sofar.NightState inverterNightState) 
        {
            if (inverterNightState == Device.Sofar.NightState.Night)
            {
                return "Off";
            }
            else
            {
                return OperatingState.ToString();
            }
        }

        internal string IsFaultToReport()
        {
            if (OperatingState != OperatingState.Normal && OperatingState != OperatingState.Waiting && OperatingState != OperatingState.Checking)
            {
                return ToCsv();
            }
            else
            {
                return null;
            }
        }
    }
}
