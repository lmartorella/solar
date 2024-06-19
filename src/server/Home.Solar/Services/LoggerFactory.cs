using System;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Console logger
    /// </summary>
    internal class LoggerFactory : ServiceBase, ILoggerFactory
    {
        public class ConsoleLogger : ILogger
        {
            private readonly string _name;
            public string SubKey { get; set; }

            public ConsoleLogger(string name, string subKey = null)
            {
                _name = name;
                SubKey = subKey;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                var line = string.Format(ts + " " + type + "|" + _name + (SubKey != null ? "-" + SubKey : "") + ": " + message, args);
                Console.WriteLine(line);
            }

            public void LogFormatErr(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                var line = string.Format(ts + " " + type + "|" + _name + (SubKey != null ? "-" + SubKey : "") + ": " + message, args);
                Console.Error.WriteLine(line);
            }
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name)
        {
            return new ConsoleLogger(name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, bool verbose)
        {
            return new ConsoleLogger(name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, string subKey)
        {
            return new ConsoleLogger(name, subKey);
        }
    }
}
