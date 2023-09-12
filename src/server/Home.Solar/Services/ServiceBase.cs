namespace Lucky.Home.Services
{
    /// <summary>
    /// Base class for singleton services
    /// </summary>
    public class ServiceBase : IService
    {
        private ILogger _logger;
        private bool _verboseLog;
        protected string LogName { get; private set; }

        protected ServiceBase(bool verboseLog = false)
        {
            _verboseLog = verboseLog;
            LogName = GetType().Name;
            if (LogName.EndsWith("Service"))
            {
                LogName = LogName.Substring(0, LogName.Length - "Service".Length);
            }
        }

        public ILogger Logger 
        { 
            get 
            {
                if (_logger == null)
                {
                    _logger = Manager.GetService<ILoggerFactory>().Create(LogName, _verboseLog);
                }
                return _logger;
            } 
        }

        public virtual void Dispose()
        { }
    }
}
