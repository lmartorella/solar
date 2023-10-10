namespace Lucky.Home.Solar
{
    /// <summary>
    /// Known inverter states. Unknown state will be logged with original flags
    /// </summary>
    static public class InverterStates
    {
        public const string Normal = "";
        public const string Off = "OFF";
        public const string NoGrid = "NOGRID";

        public const string Waiting = "WAIT";
        public const string Checking= "CHK";

        internal static bool IsFault(string state)
        {
            return state != Normal && state != Off && state != Waiting && state != Checking;
        }

        internal static string ToFault(string state)
        {
            return IsFault(state) ? state : null;
        }
    }
}
