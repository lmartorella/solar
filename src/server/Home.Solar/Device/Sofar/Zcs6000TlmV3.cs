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
                args.IsModbusConnected = false;
            }
            else
            {
                var data = await GetData();
                args.CommunicationError = data.Item2;
                args.IsModbusConnected = true;
                if (data.Item2 == CommunicationError.None)
                {
                    await PublishData(data.Item1);
                }
                else if (data.Item2 == CommunicationError.ChannelError)
                {
                    modbusClient.Disconnect();
                    args.IsModbusConnected = false;
                }
            }
        }

        private async Task PublishData(PowerData data)
        {
            await mqttService.JsonPublish(InverterDevice.SolarDataTopicId, data);
        }

        private class GridRegistryValues : RegistryValues
        {
            public GridRegistryValues(int modbusNodeId, ILogger logger)
                :base(new AddressRange { Start = 0x484, End = 0x48e }, modbusNodeId, logger)
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
            public StringsRegistryValues(int modbusNodeId, ILogger logger)
                :base(new AddressRange { Start = 0x584, End = 0x589 }, modbusNodeId, logger)
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

        private class ProductionRegistryValues : RegistryValues
        {
            public ProductionRegistryValues(int modbusNodeId, ILogger logger)
                : base(new AddressRange { Start = 0x684, End = 0x687 }, modbusNodeId, logger)
            {
            }

            public double DailyProductionWh
            {
                get
                {
                    return ((GetValueAt(0x684) << 16) + GetValueAt(0x685)) * 10.0;
                }
            }

            public double TotalProductionKwh
            {
                get
                {
                    return ((GetValueAt(0x686) << 16) + GetValueAt(0x687)) * 100.0 / 1000.0;
                }
            }
        }

        private class StateRegistryValues : RegistryValues
        {
            /// <summary>
            /// Still to be reverse-engineered. 
            /// Trying to use the ones described in the "Sofarsolar ModBus RTU Communication Protocol" pdf
            /// </summary>
            private const int LikelyFaultBitsWindowSize = 6;

            public StateRegistryValues(int modbusNodeId, ILogger logger)
                : base(new AddressRange { Start = 0x404, End = 0x405 + LikelyFaultBitsWindowSize - 1 }, modbusNodeId, logger)
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

        private async Task<Tuple<PowerData, CommunicationError>> GetData()
        {
            var data = new PowerData();
            CommunicationError error = CommunicationError.None;

            var gridData = new GridRegistryValues(modbusNodeId, Logger);
            var stringsData = new StringsRegistryValues(modbusNodeId, Logger);
            var stateData = new StateRegistryValues(modbusNodeId, Logger);
            var prodData = new ProductionRegistryValues(modbusNodeId, Logger);

            try
            {
                var errors = 0;
                // Aggregate data in order to minimize the block readings
                errors += (await gridData.ReadData(modbusClient)) ? 0 : 1;
                errors += (await stringsData.ReadData(modbusClient)) ? 0 : 1;
                errors += (await stateData.ReadData(modbusClient)) ? 0 : 1;
                errors += (await prodData.ReadData(modbusClient)) ? 0 : 1;

                if (errors == 4)
                {
                    error = CommunicationError.TotalLoss;
                }
                else if (errors > 0)
                {
                    error = CommunicationError.PartialLoss;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout waiting data from the gateway. Since the TCP gateway uses a shorter (500ms) timeout for 
                // RTU issues, this means that the gateway has issues.
                // In addition, the FluentModbus implementation of timeouts closes the TCP clonnection, so now the channel should be reopened
                Logger.Log("CancelledReadMsg");
                // If persisting, this will cause modbus link down, so for now it is partial
                error = CommunicationError.ChannelError;
            }
            catch (Exception exc) when (exc is ObjectDisposedException || exc is IOException)
            {
                // It can happen when the TCP socket is dead. Reconnect.
                Logger.Log("ModbusIoExecReadMsg", "type", exc.GetType());
                // If persisting, this will cause modbus link down, so for now it is partial
                error = CommunicationError.ChannelError;
            }

            if (error == CommunicationError.None)
            {
                data.GridCurrentA = gridData.CurrentA;
                data.GridVoltageV = gridData.VoltageV;
                data.GridFrequencyHz = gridData.FrequencyHz;
                data.PowerW = gridData.PowerW;

                data.String1CurrentA = stringsData.String1CurrentA;
                data.String1VoltageV = stringsData.String1VoltageV;
                data.String2CurrentA = stringsData.String2CurrentA;
                data.String2VoltageV = stringsData.String2VoltageV;

                data.EnergyTodayWh = prodData.DailyProductionWh;
                data.TotalEnergyKWh = prodData.TotalProductionKwh;

                data.InverterState = stateData.StateStr;
                data.TimeStamp = DateTime.Now;
            }

            return Tuple.Create(data, error);
        }
    }
}
