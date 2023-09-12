using System;

namespace Lucky.Home.Services
{
    public class MqttRemoteCallError : Exception
    {
        public MqttRemoteCallError(string message)
            :base(message)
        {
        }
    }
}
