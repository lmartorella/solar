using FluentModbus;
using Lucky.Home.Services;
using System;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ModbusClient = Lucky.Home.Services.ModbusClient;

namespace Lucky.Home.Device.Sofar
{
    internal class RegistryValues
    {
        private readonly AddressRange Addresses;
        private readonly int modbusNodeId;
        private readonly ILogger logger;
        private ushort[] Data;

        public RegistryValues(AddressRange addresses, int modbusNodeId, ILogger logger)
        {
            Addresses = addresses;
            this.modbusNodeId = modbusNodeId;
            this.logger = logger;
        }

        /// <summary>
        /// Returns false in case of timeout
        /// </summary>
        public async Task<bool> ReadData(ModbusClient client)
        {
            try
            {
                Data = await client.ReadHoldingRegistries(modbusNodeId, Addresses.Start, Addresses.End - Addresses.Start + 1);
                return true;
            }
            catch (ModbusException exc)
            {
                logger.Log("ModbusExc", "message", exc.Message);
                // The inverter responded with some error that is not managed, so it is alive
                // Even the RTU timeout is managed by the gateway and translated to a modbus error
                return false;
            }
        }

        public ushort GetValueAt(int address)
        {
            return Data[address - Addresses.Start];
        }
    }
}
