using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Net
{
    public class PipeJsonServer<TReq, TResp> : IJsonWebServer<TReq, TResp>
    {
        private string _path;
        private DataContractJsonSerializer _reqSer;
        private DataContractJsonSerializer _respSer;

        public PipeJsonServer(string path, Type[] additionalTypes)
        {
            _path = path;
            _reqSer = new DataContractJsonSerializer(typeof(TReq), additionalTypes);
            _respSer = new DataContractJsonSerializer(typeof(TResp), additionalTypes);
        }

        public Func<TReq, Task<Tuple<TResp, bool>>> ManageRequest { get; set; }

        public async Task Start()
        {
            bool quitLoop = false;
            while (!quitLoop)
            {
                NamedPipeServerStream stream = null;
                try
                {
                    // Open named pipe
                    stream = new NamedPipeServerStream(@"\\.\" + _path);
                    await stream.WaitForConnectionAsync();
                    TReq req;
                    //var req = reqSer.ReadObject(stream) as WebRequest;
                    {
                        var r = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                        var buf = await r.ReadLineAsync();
                        req = (TReq)_reqSer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(buf)));
                    }

                    var resp = await ManageRequest(req);
                    _respSer.WriteObject(stream, resp.Item1);
                    // New line
                    await stream.WriteAsync(new byte[] { 10, 13 }, 0, 2);
                    await stream.FlushAsync();
                    stream.WaitForPipeDrain();
                    stream.Disconnect();

                    quitLoop = resp.Item2;
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
        }
    }
}
