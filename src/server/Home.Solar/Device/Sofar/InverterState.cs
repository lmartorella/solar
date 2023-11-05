namespace Lucky.Home.Device.Sofar
{
    public enum InverterState
    {
        /// <summary>
        /// Inverter not responding to modbus for some time (e.g. 5 minutes), or modbus bridge missing.
        /// Considered Night Mode. Send notification of total energy when enter in Off state.
        /// </summary>
        Off,

        /// <summary>
        /// Working and receiving data
        /// </summary>
        Online
    }
}
