using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Topic ID: notification/send_mail
    /// </summary>
    [DataContract]
    public class SendMailRequestMqttPayload
    {
        [DataMember(Name = "title")]
        public string Title = null!;

        [DataMember(Name = "body")]
        public string Body = null!;

        [DataMember(Name = "isAdminReport")]
        public bool IsAdminReport;
    }
    
    class NotificationService : ServiceBase, INotificationService
    {
        private readonly MqttService mqttService;

        public NotificationService() : base(false)
        { 
            mqttService = Manager.GetService<MqttService>();
        }

        public IStatusUpdate EnqueueStatusUpdate(string groupTitle, string message)
        {
            throw new System.NotImplementedException();
        }

        public Task SendMail(string title, string body, bool isAdminReport)
        {
            // Post
            return mqttService.JsonPublish("notification/send_mail", new SendMailRequestMqttPayload() 
            {
                Body = body,
                Title = title,
                IsAdminReport = isAdminReport
            });
        }
    }
}
