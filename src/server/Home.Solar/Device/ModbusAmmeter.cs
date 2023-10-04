﻿using Lucky.Home.Services;
using System.Threading.Tasks;
using System;
using Lucky.Home.Solar;
using System.Text;

namespace Lucky.Home.Device
{
    internal class ModbusAmmeter
    {
        private readonly int modbusNodeId;
        private readonly ModbusClient modbusClient;
        private readonly MqttService mqttService;
        private static readonly TimeSpan Period = TimeSpan.FromSeconds(1.5);

        public ModbusAmmeter(string deviceHostName, int modbusNodeId)
        {
            this.modbusNodeId = modbusNodeId;
            if (deviceHostName != "")
            {
                modbusClient = Manager.GetService<ModbusClientFactory>().Get(deviceHostName, FluentModbus.ModbusEndianness.BigEndian);
            }
            mqttService = Manager.GetService<MqttService>();
        }

        public async Task StartLoop()
        {
            // Start wait loop
            while (true)
            {
                await Task.Delay(Period);
                await PullData();
            }
        }

        private async Task PullData()
        {
            // Check TCP MODBUS connection
            if (modbusClient == null || !modbusClient.CheckConnected())
            {
                await PublishData(null);
            }
            else
            {
                await PublishData(await GetData());
            }
        }

        private async Task<double?> GetData()
        {
            var buffer = await modbusClient.ReadHoldingRegistriesFloat(modbusNodeId, 0, 1);
            return buffer[0];
        }

        private async Task PublishData(double? data)
        {
            if (data != null)
            {
                await mqttService.RawPublish(AnalogIntegrator.DataTopicId, Encoding.UTF8.GetBytes(data.ToString()));
            }
            else
            {
                await mqttService.RawPublish(AnalogIntegrator.DataTopicId, new byte[0]);
            }
        }
    }
}