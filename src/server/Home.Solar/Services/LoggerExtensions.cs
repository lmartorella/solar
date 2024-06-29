using System;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Extension methods to log 
    /// </summary>
    public static class LoggerExtensions
    {
        private const string WARN = "WARN";
        private const string ERROR = "ERR ";
        private const string INFO = "info";
        private const string EXC = "EXC ";

        public static void Log(this ILogger logger, string message)
        {
            logger.LogFormat(INFO, message);
        }

        public static void Log(this ILogger logger, string message, string param1, object value1)
        {
            logger.LogFormat(INFO, "{0} [{1}]: {2}", message, param1, value1);
        }

        public static void Log(this ILogger logger, string message, string param1, object value1, string param2, object value2)
        {
            logger.LogFormat(INFO, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public static void Log(this ILogger logger, string message, string param1, object value1, string param2, object value2, string param3, object value3)
        {
            logger.LogFormat(INFO, "{0} [{1}]: {2} [{3}]: {4} [{5}]: {6}", message, param1, value1, param2, value2, param3, value3);
        }

        public static void Warning(this ILogger logger, string message)
        {
            logger.LogFormat(WARN, message);
        }

        public static void Warning(this ILogger logger, string message, string param1, object value1)
        {
            logger.LogFormat(WARN, "{0} [{1}]: {2}", message, param1, value1);
        }

        public static void Warning(this ILogger logger, string message, string param1, object value1, string param2, object value2)
        {
            logger.LogFormat(WARN, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public static void Warning(this ILogger logger, string message, string param1, object value1, string param2, object value2, string param3, object value3)
        {
            logger.LogFormat(WARN, "{0} [{1}]: {2} [{3}]: {4}, [{5}]: {6}", message, param1, value1, param2, value2, param3, value3);
        }

        public static void Error(this ILogger logger, string message, string param1, object value1, string param2, object value2)
        {
            logger.LogFormat(ERROR, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.LogFormat(ERROR, message);
        }

        public static void Error(this ILogger logger, string message, string param1, object value1)
        {
            logger.LogFormat(ERROR, "{0} [{1}]: {2}", message, param1, value1);
        }

        public static void Exception(this ILogger logger, Exception exc, bool fullStack = true)
        {
            logger.LogFormatErr(EXC, "{2}: {0}: Stack: {1}", exc.Message, fullStack ? UnrollStack(exc) : "", exc.GetType().Name);
        }

        public static void Exception(this ILogger logger, Exception exc, string context, bool fullStack = true)
        {
            logger.LogFormatErr(EXC, "{2} at {3}: {0}: Stack: {1}", exc.Message, fullStack ? UnrollStack(exc) : "", exc.GetType().Name, context);
        }

        private static string UnrollStack(Exception exc)
        {
            return exc.StackTrace + ((exc.InnerException != null) ? (Environment.NewLine + exc.InnerException.GetType().Name + ": Inner exc: " + exc.InnerException.Message + UnrollStack(exc.InnerException)) : String.Empty);
        }
    }
}
