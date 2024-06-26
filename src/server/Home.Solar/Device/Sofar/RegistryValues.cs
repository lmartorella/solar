﻿using FluentModbus;
using Lucky.Home.Services;
using System.Threading.Tasks;
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
        /// Returns false in case of timeout or other Modbus errors.
        /// In night mode, silence logging of timeout errors
        /// </summary>
        public async Task<bool> ReadData(ModbusClient client, bool nightMode)
        {
            try
            {
                Data = await client.ReadHoldingRegistries(modbusNodeId, Addresses.Start, Addresses.End - Addresses.Start + 1);
                return true;
            }
            catch (ModbusException exc)
            {
                if (exc.ExceptionCode != ModbusExceptionCode.GatewayTargetDeviceFailedToRespond || !nightMode)
                {
                    logger.Log("ModbusExc", "message", exc.Message);
                }
                // The inverter RTU-to-TCP responded with some error that is not managed, so it is alive
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
