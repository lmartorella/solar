using System;
using Lucky.Home.Services;
using System.Threading.Tasks;
using Lucky.Home.Application;
using Lucky.Home;

[assembly: Application(typeof (HomeApp))]

namespace Lucky.Home.Application
{
    /// <summary>
    /// The home application
    /// </summary>
    class HomeApp : ServiceBase, IApplication
    {
        public async Task Start()
        {
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
            _ = Manager.GetService<MqttService>().SubscribeRpc("kill", async (RpcVoid _) =>
            {
                await Task.Delay(1500);
                Manager.Kill(Logger, "killed by parent process");
                return new RpcVoid();
            });
        }
    }
}
