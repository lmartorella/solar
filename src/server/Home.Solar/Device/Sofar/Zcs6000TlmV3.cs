using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModbusClient = Lucky.Home.Services.ModbusClient;

namespace Lucky.Home.Device.Sofar
{
    /// <summary>
    /// Connects to the Zucchetti/Sofar Inverter via TCP Modbus
    /// </summary>
    class Zcs6000TlmV3
    {
        private MqttService mqttService;
        private readonly ModbusClient modbusClient;
        private int modbusNodeId;
        private readonly PollStrategyManager pollStrategyManager;

        public Zcs6000TlmV3(PollStrategyManager pollStrategyManager, string deviceHostName, int modbusNodeId)
        {
            this.modbusNodeId = modbusNodeId;
            this.pollStrategyManager = pollStrategyManager;

            modbusClient = Manager.GetService<ModbusClientFactory>().Get(deviceHostName, FluentModbus.ModbusEndianness.BigEndian);
            pollStrategyManager.PullData += (o, e) =>
            {
                e.Task = PullData(e);
            };
            mqttService = Manager.GetService<MqttService>();
        }

        public Task StartLoop()
        {
            return pollStrategyManager.StartLoop();
        }

        private async Task PullData(PollStrategyManager.PullDataEventArgs args)
        {
            // Publish the state machine state
            await mqttService.RawPublish(InverterDevice.SolarStateTopicId, Encoding.UTF8.GetBytes(args.State.ToString()));

            // Check TCP MODBUS connection
            if (!modbusClient.CheckConnected())
            {
                args.IsConnected = false;
            }
            else
            {
                var data = await GetData();
                args.DataValid = data != null;
                args.IsConnected = true;
                await PublishData(data);
            }
        }

        private async Task PublishData(PowerData data)
        {
            if (data != null)
            {
                await mqttService.JsonPublish(InverterDevice.SolarDataTopicId, data);
            }
        }

        private class AddressRange
        {
            public int Start;
            public int End; // inclusive
        }

        private class RegistryValues
        {
            private readonly AddressRange Addresses;
            private readonly int modbusNodeId;
            private ushort[] Data;

            public RegistryValues(AddressRange addresses, int modbusNodeId)
            {
                Addresses = addresses;
                this.modbusNodeId = modbusNodeId;
            }

            public async Task ReadData(ModbusClient client)
            {
                Data = await client.ReadHoldingRegistries(modbusNodeId, Addresses.Start, Addresses.End - Addresses.Start + 1);
            }

            public ushort GetValueAt(int address)
            {
                return Data[address - Addresses.Start];
            }
        }

        private class GridRegistryValues : RegistryValues
        {
            public GridRegistryValues(int modbusNodeId)
                :base(new AddressRange { Start = 0x484, End = 0x48e }, modbusNodeId)
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
            public StringsRegistryValues(int modbusNodeId)
                :base(new AddressRange { Start = 0x584, End = 0x589 }, modbusNodeId)
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
            public ChargeRegistryValues(int modbusNodeId)
                : base(new AddressRange { Start = 0x426, End = 0x426 }, modbusNodeId)
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
            try
            {
                var data = new PowerData();

                // Aggregate data in order to minimize the block readings
                var gridData = new GridRegistryValues(modbusNodeId);
                await gridData.ReadData(modbusClient);
                var stringsData = new StringsRegistryValues(modbusNodeId);
                await stringsData.ReadData(modbusClient);
                var chargeData = new ChargeRegistryValues(modbusNodeId);
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
            catch (TimeoutException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
