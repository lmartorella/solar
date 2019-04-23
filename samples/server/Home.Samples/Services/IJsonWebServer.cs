using System;
using System.Threading.Tasks;

namespace Lucky.Net
{
    /// <summary>
    /// Interface for a JSON based web server (type serialized via DataContractJsonSerializer)
    /// </summary>
    public interface IJsonWebServer<Treq, TResp>
    {
        /// <summary>
        /// Start pipe server
        /// </summary>
        /// <returns></returns>
        Task Start();

        /// <summary>
        /// Manage input messages (Treq), respond async with Tresp and a boolean (when true, exit the server)
        /// </summary>
        Func<Treq, Task<Tuple<TResp, bool>>> ManageRequest { get; set; }
    }
}
