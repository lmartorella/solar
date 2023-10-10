using FluentModbus;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.IO;
using System.Text;
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
        private readonly ILogger Logger;

        public Zcs6000TlmV3(PollStrategyManager pollStrategyManager, string deviceHostName, int modbusNodeId)
        {
            this.modbusNodeId = modbusNodeId;
            this.pollStrategyManager = pollStrategyManager;
            Logger = Manager.GetService<LoggerFactory>().Create("Zcs");

            modbusClient = Manager.GetService<ModbusClientFactory>().Get(deviceHostName, FluentModbus.ModbusEndianness.BigEndian);
            pollStrategyManager.PullData += (o, e) =>
            {
                e.Task = PullData(e);
            };
            mqttService = Manager.GetService<MqttService>();
            Logger.Log("Start", "host", deviceHostName + ":" + modbusNodeId);
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
            /// <summary>
            /// Base 0
            /// </summary>
            public int Start;

            /// <summary>
            /// Base 0, included
            /// </summary>
            public int End;
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

        private class StateRegistryValues : RegistryValues
        {
            /// <summary>
            /// To be analyzed. Trying to use the ones described in the "Sofarsolar ModBus RTU Communication Protocol" pdf
            /// </summary>
            private const int LikelyFaultBitsWindowSize = 6;

            public StateRegistryValues(int modbusNodeId)
                : base(new AddressRange { Start = 0x404, End = 0x405 + LikelyFaultBitsWindowSize - 1 }, modbusNodeId)
            {
            }

            public string StateStr
            {
                get
                {
                    switch (OperatingState)
                    {
                        case 0:
                            return InverterStates.Waiting;
                        case 1:
                            return InverterStates.Checking;
                        case 2:
                            return InverterStates.Normal;
                        case 3:
                            return "FAULT:" + FaultBits;
                        case 4:
                            return "PERM_FAULT:" + FaultBits;
                        default:
                            return "UNKNOWN:" + OperatingState;
                    }
                }
            }

            private int OperatingState
            {
                get
                {
                    return GetValueAt(0x404);
                }
            }

            private string FaultBits
            {
                get
                {
                    StringBuilder str = new StringBuilder();
                    bool first = true;
                    for (int a = 0x405; a < 0x405 + LikelyFaultBitsWindowSize; a++)
                    {
                        if (!first)
                        {
                            str.Append(",");
                            first = false;
                        }
                        str.Append(GetValueAt(a).ToString("x4"));
                    }
                    return str.ToString();
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
                var stateData = new StateRegistryValues(modbusNodeId);
                await stateData.ReadData(modbusClient);

                data.GridCurrentA = gridData.CurrentA;
                data.GridVoltageV = gridData.VoltageV;
                data.GridFrequencyHz = gridData.FrequencyHz;
                data.PowerW = gridData.PowerW;

                data.String1CurrentA = stringsData.String1CurrentA;
                data.String1VoltageV = stringsData.String1VoltageV;
                data.String2CurrentA = stringsData.String2CurrentA;
                data.String2VoltageV = stringsData.String2VoltageV;

                data.EnergyTodayWh = chargeData.ChargeAh * data.GridVoltageV;

                data.InverterState = stateData.StateStr;
                data.TotalEnergyKWh = 0;
                data.TimeStamp = DateTime.Now;

                return data;
            }
            catch (OperationCanceledException)
            {
                Logger.Log("ModbusTimeoutReadMsg");
                return null;
            }
            catch (IOException)
            {
                Logger.Log("ModbusIoExecReadMsg");
                return null;
            }
            catch (ModbusException exc)
            {
                Logger.Log("ModbusExc", "message", exc.Message);
                return null;
            }
        }
    }
}
