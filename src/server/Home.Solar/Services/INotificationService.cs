using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Service for notifications (e.g. emails...)
    /// </summary>
    public interface INotificationService : IService
    {
        /// <summary>
        /// Send an immediate text mail (not coalesced)
        /// </summary>
        Task SendMail(string title, string body, bool isAdminReport);

        /// <summary>
        /// Enqueue a low-priority notification (sent aggregated on throttle basis).
        /// `messageToAppend` will be appended.
        /// `altMessageToAppendIfStillInQueue` will be used instead of `messageToAppend` if the message is still in queue, if passed.
        /// </summary>
        void EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null);
    }

    /// <summary>
    /// Configuration for email SMTP service
    /// </summary>
    [DataContract]
    internal class MailConfiguration
    {
        public MailConfiguration()
        {
            SmtpHost = "smtp.host.com";
            SmtpPort = 25;
            Sender = "net@mail.com";
            User = "user";
            Password = "password";
            NotificationRecipient = "user@mail.com";
            AdminNotificationRecipient = "admin@mail.com";
        }

        [DataMember]
        public string SmtpHost { get; set; }

        [DataMember]
        public int SmtpPort { get; set; }

        [DataMember]
        public bool EnableSsl { get; set; }

        [DataMember]
        public string Sender { get; set; }

        [DataMember]
        public string User { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string NotificationRecipient { get; set; }

        [DataMember]
        public string AdminNotificationRecipient { get; set; }
    }
}
