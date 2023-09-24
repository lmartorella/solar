using FluentModbus;
using System;
using System.Collections.Generic;

namespace Lucky.Home.Services
{
    internal class ModbusClientFactory : ServiceBase
    {
        private readonly Dictionary<string, ModbusClient> clients = new Dictionary<string, ModbusClient>();

        public ModbusClient Get(string deviceHostName, ModbusEndianness endianness)
        {
            ModbusClient client;
            if (!clients.TryGetValue(deviceHostName, out client))
            {
                client = new ModbusClient(deviceHostName, endianness);
                clients[deviceHostName] = client;
            }
            else
            {
                if (client.Endianness != endianness)
                {
                    throw new InvalidOperationException("Endianness cannot be changed");
                }
            }
            return client;
        }
    }
}
