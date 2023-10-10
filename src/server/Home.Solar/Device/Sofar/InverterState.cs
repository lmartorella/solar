namespace Lucky.Home.Device.Sofar
{
    public enum InverterState
    {
        /// <summary>
        /// Inverter not responding to modbus
        /// </summary>
        Off,

        /// <summary>
        /// Modbus bridge missing
        /// </summary>
        ModbusConnecting,

        /// <summary>
        /// Working
        /// </summary>
        Online
    }
}
