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
    
    /// <summary>
    /// Topic ID: notification/enqueue_status_update
    /// </summary>
    [DataContract]
    public class EnqueueStatusUpdateRequestMqttPayload
    {
        [DataMember(Name = "groupTitle")]
        public string GroupTitle = null!;

        [DataMember(Name = "messageToAppend")]
        public string MessageToAppend = null!;

        [DataMember(Name = "altMessageToAppendIfStillInQueue")]
        public string AltMessageToAppendIfStillInQueue;
    }

    class NotificationService : ServiceBase, INotificationService
    {
        private readonly MqttService mqttService;

        public NotificationService() : base(false)
        { 
            mqttService = Manager.GetService<MqttService>();
        }

        public Task EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null)
        {
            // Post
            return mqttService.JsonPublish("notification/enqueue_status_update", new EnqueueStatusUpdateRequestMqttPayload() 
            {
                GroupTitle = groupTitle,
                MessageToAppend = messageToAppend,
                AltMessageToAppendIfStillInQueue = altMessageToAppendIfStillInQueue
            });
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
