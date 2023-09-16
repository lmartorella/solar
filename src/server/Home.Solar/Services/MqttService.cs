using Lucky.Home.Solar;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Implements plain topic subscribe/publish and RPCs.
    /// Errors are sent using the content-type "application/net_err+text"
    /// Both typed JSON messages and raw supports null/empty payload to transfer nulls/void.
    /// </summary>
    public class MqttService : ServiceBase
    {
        private readonly MqttFactory mqttFactory;
        private readonly IManagedMqttClient mqttClient;

        public MqttService()
        {
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateManagedMqttClient();
            _ = Connect();
            mqttClient.ConnectedAsync += (e) =>
            {
                Logger.Log("Connected");
                return Task.CompletedTask;
            };
            mqttClient.DisconnectedAsync += (e) =>
            {
                Logger.Log("Disconnected, reconnecting", "reason", e.ReasonString);
                return Task.CompletedTask;
            };
        }

        private async Task Connect()
        {
            var clientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(3.5))
                .WithMaxPendingMessages(1)
                .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                .WithClientOptions(new MqttClientOptionsBuilder()
                  .WithClientId(Assembly.GetEntryAssembly().GetName().Name)
                  .WithTcpServer("localhost")
                  .WithWillTopic(UserInterface.Topic)
                  .WithWillPayload(UserInterface.WillPayload)
                  .Build())
                .Build();
            await mqttClient.StartAsync(clientOptions);
            Logger.Log("Started");
        }

        /// <summary>
        /// Subscribe a raw binary MQTT topic 
        /// Doesn't support errors
        /// </summary>
        public async Task SubscribeRawTopic(string topic, Action<byte[]> handler)
        {
            var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
            await mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                var msg = args.ApplicationMessage;
                if (msg.Topic == topic)
                {
                    handler(msg.PayloadSegment.Array);
                }
                return Task.FromResult(null as byte[]);
            };
        }

        /// <summary>
        /// Subscribe a MQTT topic that talks JSON. 
        /// Doesn't support errors
        /// </summary>
        public Task SubscribeJsonTopic<T>(string topic, Action<T> handler) where T: class, new() 
        {
            var deserializer = new DataContractJsonSerializer(typeof(T));
            return SubscribeRawTopic(topic, msg =>
            {
                T req = null;
                if (msg.Length> 0)
                {
                    req = (T)deserializer.ReadObject(new MemoryStream(msg));
                }
                handler(req);
            });
        }

        /// <summary>
        /// Send raw MQTT binary topic. 
        /// Doesn't support errors
        /// </summary>
        public async Task RawPublish(string topic, byte[] value)
        {
            if (!mqttClient.IsConnected)
            {
                throw new InvalidOperationException("Broker not connected");
            }

            var message = mqttFactory.CreateApplicationMessageBuilder()
                .WithPayload(value)
                .WithTopic(topic).
                Build();
            await mqttClient.InternalClient.PublishAsync(message);
        }

        /// <summary>
        /// Send MQTT topic that talks JSON. 
        /// Doesn't support errors
        /// </summary>
        public Task JsonPublish<T>(string topic, T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream();
            serializer.WriteObject(stream, value);
            return RawPublish(topic, stream.ToArray());
        }
    }
}
