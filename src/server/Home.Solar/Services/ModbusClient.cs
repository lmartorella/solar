using FluentModbus;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    internal class ModbusClient
    {
        private readonly ModbusTcpClient client = new ModbusTcpClient();
        private readonly ILogger Logger;
        private bool _connecting;
        private readonly string _deviceHostName;
        public readonly ModbusEndianness Endianness;
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

        public ModbusClient(string deviceHostName, ModbusEndianness endianness)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("ModbusClient");
            _deviceHostName = deviceHostName;
            this.Endianness = endianness;
        }

        private async Task StartConnect()
        {
            if (_connecting)
            {
                return;
            }
            _connecting = true;

            try
            {
                IPAddress address = null;
                try
                {
                    address = (await Dns.GetHostEntryAsync(_deviceHostName)).AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault();
                }
                catch (Exception err)
                {
                    Logger.Log("DnsErr", "resolving", _deviceHostName, "err", err.Message);
                    return;
                }

                if (address == null)
                {
                    Logger.Log("DnsErr", "resolving", _deviceHostName, "err", "no result");
                    return;
                }

                client.ConnectTimeout = (int)ConnectTimeout.TotalMilliseconds;

                try
                {
                    client.Connect(address, Endianness);
                }
                catch (Exception err)
                {
                    Logger.Log("ModbusConnect", "connectingTo", address, "err", err.Message);
                    return;
                }
            }
            finally
            {
                _connecting = false;
            }
        }

        public bool CheckConnected()
        {
            // Check TCP MODBUS connection
            if (!client.IsConnected)
            {
                _ = StartConnect();
                return false;
            }
            else
            {
                _connecting = false;
                return true;
            }
        }

        public async Task<ushort[]> ReadHoldingRegistries(int unitId, int addressStart, int count)
        {
            return (await client.ReadHoldingRegistersAsync<ushort>(unitId, addressStart, count).WaitAsync(TimeSpan.FromMilliseconds(1000))).ToArray();
        }

        public async Task<float[]> ReadHoldingRegistriesFloat(int unitId, int addressStart, int count)
        {
            return (await client.ReadHoldingRegistersAsync<float>(unitId, addressStart, count).WaitAsync(TimeSpan.FromMilliseconds(1000))).ToArray();
        }
    }
}
