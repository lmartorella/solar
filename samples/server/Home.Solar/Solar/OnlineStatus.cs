using System.Runtime.Serialization;

namespace Lucky.Home.Solar
{
    [DataContract]
    public enum OnlineStatus
    {
        /// <summary>
        /// Full online
        /// </summary>
        Online = 1,

        /// <summary>
        /// Critical services offline
        /// </summary>
        Offline = 2,

        /// <summary>
        /// Critical services online, optional services offline
        /// </summary>
        PartiallyOnline = 3
    }
}
