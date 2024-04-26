namespace Lucky.Home.Device.Sofar
{
    public enum NightState
    {
        /// <summary>
        /// Inverter not responding to modbus for some time (e.g. 5 minutes), or modbus bridge missing.
        /// Considered Night Mode. Send notification of total energy when enter in Off state.
        /// </summary>
        Night,

        /// <summary>
        /// Working and receiving data
        /// </summary>
        Running
    }
}
