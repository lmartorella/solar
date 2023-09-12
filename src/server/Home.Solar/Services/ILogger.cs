namespace Lucky.Home.Services
{
    /// <summary>
    /// A logger
    /// </summary>
    public interface ILogger
    {
        string SubKey { get; set; }
        void LogFormat(string type, string message, params object[] args);
        void LogFormatErr(string type, string message, params object[] args);
    }
}
