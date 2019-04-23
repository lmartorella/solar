using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using Lucky.Net;
using Lucky.Home.Services;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    [DataContract]
    public class WebRequest
    {
        /// <summary>
        /// Can be getProgram, setImmediate
        /// </summary>
        [DataMember(Name = "command")]
        public string Command { get; set; }

        [DataMember(Name = "immediate")]
        public ImmediateZone[] ImmediateZones { get; set; }
    }

    [DataContract]
    public class ImmediateZone
    {
        [DataMember(Name = "zones")]
        public int[] Zones { get; set; }

        [DataMember(Name = "time")]
        public int Time { get; set; }

        public override string ToString()
        {
            return "Zones: " + string.Join(",", Zones) + " [Time: " + Time + "]";
        }
    }

    [DataContract]
    [KnownType(typeof(GardenWebResponse))]
    public class WebResponse
    {
        public bool CloseServer;
    }

    public class PipeServer : ServiceBase
    {
        public class MessageEventArgs : EventArgs
        {
            public WebRequest Request;
            public Task<WebResponse> Response;
        }

        public event EventHandler<MessageEventArgs> Message;

        public PipeServer()
        {
            var server = new PipeJsonServer<WebRequest, WebResponse>("NETHOME", new[] { typeof(GardenWebResponse) });
            server.ManageRequest = async req =>
            {
                var args = new MessageEventArgs() { Request = req, Response = Task.FromResult(new WebResponse()) };
                Message?.Invoke(this, args);
                var r = await args.Response;
                return Tuple.Create(r, r.CloseServer);
            };
            server.Start();
        }
    }
}
