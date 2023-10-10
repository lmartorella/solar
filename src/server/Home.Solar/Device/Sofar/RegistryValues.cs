using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ModbusClient = Lucky.Home.Services.ModbusClient;

namespace Lucky.Home.Device.Sofar
{
    internal class RegistryValues
    {
        private readonly AddressRange Addresses;
        private readonly int modbusNodeId;
        private ushort[] Data;

        public RegistryValues(AddressRange addresses, int modbusNodeId)
        {
            Addresses = addresses;
            this.modbusNodeId = modbusNodeId;
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
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public ushort GetValueAt(int address)
        {
            return Data[address - Addresses.Start];
        }
    }
}
