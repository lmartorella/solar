using Lucky.Home.Services;
using System.Globalization;
using System;
using System.Threading.Tasks;

namespace Lucky.Home
{
    public static class Bootstrap
    {
        public static async Task<ILogger> Start(string[] arguments, string appName)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("it-IT");
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

            Manager.Register<JsonIsolatedStorageService, IIsolatedStorageService>();
            Manager.Register<NotificationService, INotificationService>();

            Manager.Register<ConfigurationService, IConfigurationService>();
            Manager.GetService<ConfigurationService>().Init(arguments);

            Manager.Register<LoggerFactory, ILoggerFactory>();
            Manager.GetService<LoggerFactory>().Init(Manager.GetService<PersistenceService>());

            Manager.GetService<IIsolatedStorageService>().InitAppRoot(appName);

            var logger = Manager.GetService<ILoggerFactory>().Create(appName + ":Main");
            logger.Log("Started");
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                logger.Exception((Exception)e.ExceptionObject);
            };

            Console.CancelKeyPress += (sender, args) =>
            {
                _ = Manager.Kill(logger, "detected CtrlBreak", TimeSpan.FromMilliseconds(10));
                args.Cancel = true;
            };

            // If a mail should be sent at startup, do it now
            var mailBody = Manager.GetService<IConfigurationService>().GetConfig("sendMailErr");
            if (!string.IsNullOrEmpty(mailBody))
            {
                // Append err file to the mail body, if avail
                string lastErrorFile = Manager.GetService<ILoggerFactory>().LastErrorText;
                if (lastErrorFile != null)
                {
                    mailBody += Environment.NewLine + Environment.NewLine + lastErrorFile;
                }
                await Manager.GetService<INotificationService>().SendMail(Resources.startupMessage, mailBody, true);
            }

            // Implements a IPC pipe with web server
            _ = Manager.GetService<MqttService>().SubscribeRawTopic(appName + "/kill", (_e) =>
            {
                _ = Manager.Kill(logger, "killed by process manager via MQTT", TimeSpan.FromSeconds(1.5));
            });


            return logger;
        }
    }
}
