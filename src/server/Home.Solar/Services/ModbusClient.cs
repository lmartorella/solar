using FluentModbus;
using Lucky.Home.Services.FluentModbus;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
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
                    Logger.Log("ModbusConnect", "connectingTo", address, "err", err.Message, "type", err.GetType().Name);
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

        private CancellationToken Timeout
        {
            get
            {
                return new CancellationTokenSource(1000).Token;
            }
        }

        public async Task<ushort[]> ReadHoldingRegistries(int unitId, int addressStart, int count)
        {
            // Buggy API not passing cancellation token. Switch to ReadHoldingRegistersAsync<T>
            // after https://github.com/Apollo3zehn/FluentModbus/pull/100 merge
            var dataset = SpanExtensions.Cast<byte, ushort>(await client.ReadHoldingRegistersAsync((byte)unitId, (ushort)addressStart, (ushort)count, Timeout));
            if (/*client.SwapBytes*/ true)
            {
                ModbusUtils.SwitchEndianness(dataset);
            }
            return dataset.ToArray();
        }

        public async Task<float[]> ReadHoldingRegistriesFloat(int unitId, int addressStart, int count)
        {
            // Buggy API not passing cancellation token. Switch to ReadHoldingRegistersAsync<T>
            // after https://github.com/Apollo3zehn/FluentModbus/pull/100 merge
            var dataset = SpanExtensions.Cast<byte, float>(await client.ReadHoldingRegistersAsync((byte)unitId, (ushort)addressStart, (ushort)count, Timeout));
            if (/*client.SwapBytes*/ true)
            {
                ModbusUtils.SwitchEndianness(dataset);
            }
            return dataset.ToArray();
        }
    }
}
