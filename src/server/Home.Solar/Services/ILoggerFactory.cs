namespace Lucky.Home.Services
{
    /// <summary>
    /// Service to create loggers
    /// </summary>
    public interface ILoggerFactory : IService
    {
        ILogger Create(string name);
        ILogger Create(string name, string subKey);
        ILogger Create(string name, bool verbose);

        /// <summary>
        /// Used after a restart due to exception
        /// </summary>
        string LastErrorText { get; }
    }
}
