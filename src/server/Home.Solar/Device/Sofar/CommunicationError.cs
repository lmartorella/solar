namespace Lucky.Home.Device.Sofar
{
    public enum CommunicationError
    {
        /// <summary>
        /// No error
        /// </summary>
        None,

        /// <summary>
        /// Some data is lost
        /// </summary>
        PartialLoss,

        /// <summary>
        /// All data is lost. Inverter down?
        /// </summary>
        TotalLoss,

        /// <summary>
        /// The TCP channel has issues and connection should be restored
        /// </summary>
        ChannelError
    }
}
