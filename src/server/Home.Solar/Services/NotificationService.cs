using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Net.Mime;
using System.IO;

namespace Lucky.Home.Services
{
    class NotificationService : ServiceBaseWithData<MailConfiguration>, INotificationService
    {
        private class Message
        {
            public string Text { get; private set; }

            public object LockObject { get; set; }

            public void Update(string appendText)
            {
                lock (LockObject)
                {
                    Text += appendText;
                }
            }
        }

        private class Bucket
        {
            private readonly string _groupTitle;
            private readonly NotificationService _svc;
            private List<Message> _messages = new List<Message>();
            private Timer _timer;

            public Bucket(NotificationService svc, string groupTitle)
            {
                _svc = svc;
                _groupTitle = groupTitle;
            }

            internal void Enqueue(string messageToAppend, string altMessageToAppendIfStillInQueue = null)
            {
                lock (this)
                {
                    if (altMessageToAppendIfStillInQueue != null)
                    {
                        if (_messages.Count > 0)
                        {
                            _messages.Last().Update(altMessageToAppendIfStillInQueue);
                            return;
                        }
                        messageToAppend = altMessageToAppendIfStillInQueue;
                    }
                    var message = new Message();
                    message.LockObject = this;
                    _messages.Add(message);
                    // Start timer
                    if (_timer == null)
                    {
                        _timer = new Timer(_ =>
                        {
                            string msg;
                            lock (this)
                            {
                                // Send a single mail with all the content
                                msg = string.Join(Environment.NewLine, _messages.Select(m => m.Text));
                                _messages.Clear();
                                _timer = null;
                            }
                            if (msg.Trim().Length > 0)
                            {
                                _ = _svc.SendMail(_groupTitle, msg, true);
                            }
                        }, null, (int)TimeSpan.FromHours(1).TotalMilliseconds, Timeout.Infinite);
                    }
                }
            }
        }

        private Dictionary<string, Bucket> _statusBuckets = new Dictionary<string, Bucket>();

        public NotificationService() : base(true, false)
        { }

        /// <summary>
        /// Send a text mail
        /// </summary>
        public async Task SendMail(string title, string body, bool isAdminReport)
        {
            var configuration = State;

            Logger.Log("SendingMail", "title", title);

            // Specify the message content.
            MailMessage message = new MailMessage(configuration.Sender, isAdminReport ? configuration.AdminNotificationRecipient : configuration.NotificationRecipient);
            message.Subject = title;
            message.Body = body;

            bool sent = false;
            while (!sent)
            {
                sent = await TrySendMail(message);
                if (!sent)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
        }

        /// <summary>
        /// Send a HTML mail
        /// </summary>
        public async Task SendHtmlMail(string title, string htmlBody, bool isAdminReport, IEnumerable<Tuple<Stream, ContentType, string>> attachments = null)
        {
            var configuration = State;

            if (attachments == null)
            {
                attachments = new Tuple<Stream, ContentType, string>[0];
            }
            Logger.Log("SendingHtmlMail", "title", title, "attch", attachments.Count());

            // Specify the message content.
            MailMessage message = new MailMessage(configuration.Sender, isAdminReport ? configuration.AdminNotificationRecipient : configuration.NotificationRecipient);
            message.Subject = title;

            var alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);

            foreach (var attInfo in attachments)
            {
                LinkedResource resource = new LinkedResource(attInfo.Item1, attInfo.Item2);
                resource.ContentId = attInfo.Item3;
                alternateView.LinkedResources.Add(resource);
            }

            message.AlternateViews.Add(alternateView);
            message.IsBodyHtml = true;

            bool sent = false;
            while (!sent)
            {
                sent = await TrySendMail(message);
                if (!sent)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
        }

        private async Task<bool> TrySendMail(MailMessage message)
        {
            var configuration = State;

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = configuration.SmtpHost;
            client.Port = configuration.SmtpPort;
            client.Credentials = new NetworkCredential(configuration.User, configuration.Password);
            client.EnableSsl = configuration.EnableSsl;

            try
            {
                await client.SendMailAsync(message);
                Logger.Log("Mail sent to: " + message.To);
                client.Dispose();
                return true;
            }
            catch (Exception exc)
            {
                try
                {
                    client.Dispose();
                }
                catch { }
                Logger.Exception(exc, false);
                Logger.Log("Retrying in 30 seconds...");
                // Retry
                return false;
            }
        }

        public void EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null)
        {
            lock (_statusBuckets)
            {
                Bucket bucket;
                if (!_statusBuckets.TryGetValue(groupTitle, out bucket))
                {
                    bucket = new Bucket(this, groupTitle);
                    _statusBuckets.Add(groupTitle, bucket);
                }
                bucket.Enqueue(messageToAppend, altMessageToAppendIfStillInQueue);
            }
        }
    }
}
