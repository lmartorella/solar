using System;
using System.IO;
using System.Reflection;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Console logger
    /// </summary>
    internal class LoggerFactory : ServiceBase, ILoggerFactory
    {
        private string _logFile;
        private string _errFile;

        public string LastErrorText { get; private set; }

        public class ConsoleLogger : ILogger
        {
            private readonly LoggerFactory _owner;
            private readonly string _name;
            public string SubKey { get; set; }

            public ConsoleLogger(LoggerFactory owner, string name, string subKey = null)
            {
                _owner = owner;
                _name = name;
                SubKey = subKey;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                var line = string.Format(ts + " " + type + "|" + _name + (SubKey != null ? "-" + SubKey : "") + ": " + message, args);
                Console.WriteLine(line);
                _owner.AppendLogFile(_owner._logFile, line);
            }

            public void LogFormatErr(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                var line = string.Format(ts + " " + type + "|" + _name + (SubKey != null ? "-" + SubKey : "") + ": " + message, args);
                Console.WriteLine(line);
                _owner.AppendLogFile(_owner._logFile, line);
                // err.log always contains last error only
                _owner.OverwriteLogFile(_owner._errFile, line);
            }
        }

        private void AppendLogFile(string file, string line)
        {
            // Don't crash if another thread is locking the file
            lock (this)
            {
                using (StreamWriter logger = new StreamWriter(file, true))
                {
                    logger.WriteLine(line);
                }
            }
        }

        private void OverwriteLogFile(string file, string line)
        {
            lock (this)
            {
                using (StreamWriter logger = new StreamWriter(file))
                {
                    logger.WriteLine(line);
                }
            }
        }

        public void Init(PersistenceService service)
        {
            _logFile = Path.Combine(service.GetAppFolderPath(), Assembly.GetEntryAssembly().GetName().Name + ".log");
            _errFile = Path.Combine(service.GetAppFolderPath(), Assembly.GetEntryAssembly().GetName().Name + ".err");
            if (File.Exists(_errFile))
            {
                LastErrorText = File.ReadAllText(_errFile);
                File.Delete(_errFile);
            }
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name)
        {
            return new ConsoleLogger(this, name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, bool verbose)
        {
            return new ConsoleLogger(this, name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, string subKey)
        {
            return new ConsoleLogger(this, name, subKey);
        }
    }
}
