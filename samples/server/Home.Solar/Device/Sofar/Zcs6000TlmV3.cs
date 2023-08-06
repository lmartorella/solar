using FluentModbus;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Device.Sofar
{
    /// <summary>
    /// Connects to the Zucchetti/Sofar Inverter via TCP Modbus
    /// </summary>
    class Zcs6000TlmV3
    {
        private readonly PollStrategyManager _pollStrategyManager = new PollStrategyManager();
        private MqttService mqttService;
        private readonly ModbusTcpClient modbusClient = new ModbusTcpClient();
        private bool _connecting;
        private readonly ILogger Logger;

        private const string DeviceHostName = "esp32.";
        private const int ModbusNodeId = 1;

        public Zcs6000TlmV3()
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("Zcs6000TlmV3");
            _pollStrategyManager.PullData += (o, e) =>
            {
                e.Task = PullData(e);
            };
            mqttService = Manager.GetService<MqttService>();
        }

        private async Task StartConnect(TimeSpan connectTimeout)
        {
            if (_connecting)
            {
                return;
            }
            _connecting = true;

            IPAddress address = null;
            try
            {
                var list = (await Dns.GetHostEntryAsync(DeviceHostName)).AddressList;
                if (list.Length > 0)
                {
                    address = list[0];
                }
            }
            catch (Exception err)
            {
                Logger.Log("DnsErr", "resolving", DeviceHostName, "err", err.Message);
                return;
            }

            if (address == null)
            {
                Logger.Log("DnsErr", "resolving", DeviceHostName, "err", "no result");
                return;
            }

            modbusClient.ConnectTimeout = (int)connectTimeout.TotalMilliseconds;

            try
            {
                modbusClient.Connect(address, ModbusEndianness.BigEndian);
            }
            catch (Exception err)
            {
                Logger.Log("ModbusConnect", "connectingTo", address, "err", err.Message);
                return;
            }

            _connecting = false;
        }

        private async Task PullData(PollStrategyManager.PullDataEventArgs args)
        {
            // Publis the state machine state
            await mqttService.RawPublish(InverterDevice.SolarStateTopicId, Encoding.UTF8.GetBytes(args.State.ToString()));

            // Check TCP MODBUS connection
            if (!modbusClient.IsConnected)
            {
                _ = StartConnect(args.ConnectTimeout);
                args.IsConnected = false;
                return;
            }
            else
            {
                _connecting = false;
                var data = await GetData();
                args.DataValid = data != null;
            }
        }

        private async Task<PowerData> GetData()
        {
            const data = await modbusClient.ReadHoldingRegistersAsync<ushort>(ModbusNodeId, REG_ADDR, REG_COUNT);
        }
    }
}
