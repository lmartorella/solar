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

        private class AddressRange
        {
            public int Start;
            public int End; // inclusive
        }

        private class RegistryValues
        {
            private AddressRange Addresses;
            private ushort[] Data;

            public RegistryValues(AddressRange addresses)
            {
                Addresses = addresses;
            }

            public async Task ReadData(ModbusClient client)
            {
                var data = await client.ReadHoldingRegistersAsync<ushort>(ModbusNodeId, Addresses.Start, Addresses.End - Addresses.Start + 1);
                Data = data.ToArray();
            }

            public ushort GetValueAt(int address)
            {
                return Data[address - Addresses.Start];
            }
        }

        private class GridRegistryValues : RegistryValues
        {
            public GridRegistryValues()
                :base(new AddressRange { Start = 0x484, End = 0x48e })
            {
            }

            public double FrequencyHz
            {
                get
                {
                    return GetValueAt(0x484) / 100.0;
                }
            }

            public double PowerW
            {
                get
                {
                    return GetValueAt(0x485) * 10.0;
                }
            }

            public double VoltageV
            {
                get
                {
                    return GetValueAt(0x48d) / 10.0;
                }
            }

            public double CurrentA
            {
                get
                {
                    return GetValueAt(0x48e) / 100.0;
                }
            }
        }

        private class StringsRegistryValues : RegistryValues
        {
            public StringsRegistryValues()
                :base(new AddressRange { Start = 0x584, End = 0x589 })
            {
            }

            public double String1VoltageV
            {
                get
                {
                    return GetValueAt(0x584) / 10.0;
                }
            }

            public double String1CurrentA
            {
                get
                {
                    return GetValueAt(0x585) / 100.0;
                }
            }

            public double String1PowerW
            {
                get
                {
                    return GetValueAt(0x586) * 10.0;
                }
            }

            public double String2VoltageV
            {
                get
                {
                    return GetValueAt(0x587) / 10.0;
                }
            }

            public double String2CurrentA
            {
                get
                {
                    return GetValueAt(0x588) / 100.0;
                }
            }

            public double String2PowerW
            {
                get
                {
                    return GetValueAt(0x589) * 10.0;
                }
            }
        }

        private class ChargeRegistryValues : RegistryValues
        {
            public ChargeRegistryValues()
                : base(new AddressRange { Start = 0x426, End = 0x426 })
            {
            }

            public double ChargeAh
            {
                get
                {
                    return GetValueAt(0x426) * 2 / 10.0;
                }
            }
        }

        private async Task<PowerData> GetData()
        {
            var data = new PowerData();

            // Aggregate data in order to minimize the block readings
            var gridData = new GridRegistryValues();
            await gridData.ReadData(modbusClient);
            var stringsData = new StringsRegistryValues();
            await stringsData.ReadData(modbusClient);
            var chargeData = new ChargeRegistryValues();
            await chargeData.ReadData(modbusClient);

            data.GridCurrentA = gridData.CurrentA;
            data.GridVoltageV = gridData.VoltageV;
            data.GridFrequencyHz = gridData.FrequencyHz;
            data.PowerW = gridData.PowerW;

            data.String1CurrentA = stringsData.String1CurrentA;
            data.String1VoltageV = stringsData.String1VoltageV;
            data.String2CurrentA = stringsData.String2CurrentA;
            data.String2VoltageV = stringsData.String2VoltageV;

            data.EnergyTodayWh = chargeData.ChargeAh * data.GridVoltageV;

            data.InverterState = InverterStates.Normal;
            data.TotalEnergyKWh = 0;
            data.TimeStamp = DateTime.Now;

            return data;
        }
    }
}
