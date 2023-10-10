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
        /// Generic error, logged and resolved
        /// </summary>
        ManagedError
    }
}
