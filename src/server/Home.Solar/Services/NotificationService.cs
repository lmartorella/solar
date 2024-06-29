using System;
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
        private MqttService.RpcOriginator sendMailRpcOriginator;
        private MqttService.RpcOriginator statusUpdateRpcOriginator;

        public NotificationService() : base(false)
        { 
            mqttService = Manager.GetService<MqttService>();
            _ = Start();
        }

        private async Task Start()
        {
            sendMailRpcOriginator = await mqttService.RegisterRpcOriginator("notification/send_mail", TimeSpan.FromSeconds(10));
            statusUpdateRpcOriginator = await mqttService.RegisterRpcOriginator("notification/enqueue_status_update", TimeSpan.FromSeconds(10));
        }

        public async Task EnqueueStatusUpdate(string groupTitle, string messageToAppend, string altMessageToAppendIfStillInQueue = null)
        {
            await statusUpdateRpcOriginator.JsonRemoteCall<EnqueueStatusUpdateRequestMqttPayload, RpcVoid>(new EnqueueStatusUpdateRequestMqttPayload() 
            {
                GroupTitle = groupTitle,
                MessageToAppend = messageToAppend,
                AltMessageToAppendIfStillInQueue = altMessageToAppendIfStillInQueue
            });
        }

        public async Task SendMail(string title, string body, bool isAdminReport)
        {
            await sendMailRpcOriginator.JsonRemoteCall<SendMailRequestMqttPayload, RpcVoid>(new SendMailRequestMqttPayload() 
            {
                Body = body,
                Title = title,
                IsAdminReport = isAdminReport
            });
        }
    }
}
