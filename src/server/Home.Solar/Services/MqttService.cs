using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Used for empty JSON messages types
    /// </summary>
    [DataContract]
    public class RpcVoid
    {
    }

    /// <summary>
    /// Implements plain topic subscribe/publish and RPCs.
    /// Errors are sent using the content-type "application/net_err+text"
    /// Both typed JSON messages and raw supports null/empty payload to transfer nulls/void.
    /// </summary>
    public class MqttService : ServiceBase
    {
        private readonly MqttFactory mqttFactory;
        private readonly IManagedMqttClient mqttClient;
        private const string ErrContentType = "application/net_err+text";

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
                  .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                  .Build()).
                Build();
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
                throw new MqttRemoteCallError("Broker not connected");
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

        /// <summary>
        /// Enable reception of RPC calls originated by the current party
        /// </summary>
        public async Task<RpcOriginator> RegisterRpcOriginator(string topic, TimeSpan timeout)
        {
            var originator = new RpcOriginator(this, topic, timeout);
            await originator.SubscribeRawRpcResponse();
            return originator;
        }

        public class RpcOriginator
        {
            private readonly Dictionary<Guid, TaskCompletionSource<byte[]>> requests = new Dictionary<Guid, TaskCompletionSource<byte[]>>();
            private readonly MqttService owner;
            private readonly string topic;
            private readonly TimeSpan timeout;

            public RpcOriginator(MqttService owner, string topic, TimeSpan timeout)
            {
                this.owner = owner;
                this.topic = topic;
                this.timeout = timeout;
            }

            public async Task SubscribeRawRpcResponse()
            {
                var responseTopic = this.topic + "/resp";
                var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(responseTopic).Build();
                await owner.mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
                owner.mqttClient.ApplicationMessageReceivedAsync += args =>
                {
                    var msg = args.ApplicationMessage;
                    if (msg.Topic == responseTopic && msg.CorrelationData != null)
                    {
                        TaskCompletionSource<byte[]> request;
                        var correlationData = new Guid(msg.CorrelationData);
                        if (requests.TryGetValue(correlationData, out request))
                        {
                            requests.Remove(correlationData);
                            args.IsHandled = true;

                            if (msg.ContentType == ErrContentType)
                            {
                                request.TrySetException(new MqttRemoteCallError(Encoding.UTF8.GetString(msg.PayloadSegment.Array)));
                            }
                            else
                            {
                                request.TrySetResult(msg.PayloadSegment.Count > 0 ? msg.PayloadSegment.Array : null);
                            }
                        }
                    }
                    return Task.CompletedTask;
                };
                owner.Logger.Log("Subscribed responses", "topic", responseTopic);
            }

            public async Task<byte[]> RawRemoteCall(byte[] payload = null)
            {
                if (!owner.mqttClient.IsConnected)
                {
                    throw new MqttRemoteCallError("Broker not connected");
                }

                var correlationData = Guid.NewGuid();
                var request = new TaskCompletionSource<byte[]>();
                requests[correlationData] = request;
                var message = owner.mqttFactory.CreateApplicationMessageBuilder()
                    .WithCorrelationData(correlationData.ToByteArray())
                    .WithPayload(payload)
                    .WithResponseTopic(topic + "/resp")
                    .WithTopic(topic).
                    Build();
                await owner.mqttClient.InternalClient.PublishAsync(message);
                _ = Task.Delay(timeout).ContinueWith(task =>
                {
                    requests.Remove(correlationData);
                    request.TrySetCanceled();
                });
                return await request.Task;
            }

            public async Task<TResp> JsonRemoteCall<TReq, TResp>(TReq payload = null) where TReq : class, new() where TResp : class, new()
            {
                var reqSerializer = new DataContractJsonSerializer(typeof(TReq));
                var respDeserializer = new DataContractJsonSerializer(typeof(TResp));
                var requestStream = new MemoryStream();
                reqSerializer.WriteObject(requestStream, payload);

                var responseData = await RawRemoteCall(requestStream.ToArray());
                var response = (TResp)respDeserializer.ReadObject(new MemoryStream(responseData));
                return response;
            }
        }

        private async Task ProcessRpc(MqttApplicationMessage msg, Func<byte[], Task<byte[]>> handler)
        {
            byte[] responsePayload = null;
            string contentType = null;
            try
            {
                responsePayload = await handler(msg.PayloadSegment.Array);
            }
            catch (Exception exc)
            {
                // Send back error as string
                Logger.Exception(exc);
                responsePayload = Encoding.UTF8.GetBytes($"{exc.GetType().Name}: {exc.Message}");
                contentType = ErrContentType;
            }

            var respMsg = new MqttApplicationMessage()
            {
                PayloadSegment = new ArraySegment<byte>(responsePayload ?? new byte[0]),
                ContentType = contentType,
                CorrelationData = msg.CorrelationData,
                Topic = msg.ResponseTopic
            };

            await mqttClient.InternalClient.PublishAsync(respMsg);
        }

        /// <summary>
        /// Subscribe RPC requests in binary format
        /// </summary>
        public async Task SubscribeRawRpc(string topic, Func<byte[], Task<byte[]>> handler)
        {
            var mqttSubscribeOptions = new MqttTopicFilterBuilder().WithTopic(topic).Build();
            await mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                var msg = args.ApplicationMessage;
                if (msg.Topic == topic && msg.ResponseTopic != null)
                {
                    args.IsHandled = true;
                    _ = ProcessRpc(msg, handler);
                }
                return Task.CompletedTask;
            };
            Logger.Log("Subscribed requests", "topic", topic);
        }

        /// <summary>
        /// Subscribe RPC requests in JSON format
        /// </summary>
        public Task SubscribeJsonRpc<TReq, TResp>(string topic, Func<TReq, Task<TResp>> handler) where TReq: class, new() where TResp: class, new()
        {
            var reqSerializer = new DataContractJsonSerializer(typeof(TReq));
            var respDeserializer = new DataContractJsonSerializer(typeof(TResp));
            return SubscribeRawRpc(topic, async payload =>
            {
                TReq req = null;
                if (payload != null)
                {
                    req = (TReq)reqSerializer.ReadObject(new MemoryStream(payload));
                }
                TResp resp = await handler(req);
                var responseStream = new MemoryStream();
                respDeserializer.WriteObject(responseStream, resp);
                return responseStream.ToArray();
            });
        }
    }
}
