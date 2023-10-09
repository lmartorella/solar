using Lucky.Home.Services;
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
        private double? lastData = double.MaxValue;
        private readonly ILogger Logger;

        public ModbusAmmeter(string deviceHostName, int modbusNodeId)
        {
            this.modbusNodeId = modbusNodeId;
            Logger = Manager.GetService<LoggerFactory>().Create("Zcs");

            if (deviceHostName != "")
            {
                modbusClient = Manager.GetService<ModbusClientFactory>().Get(deviceHostName, FluentModbus.ModbusEndianness.BigEndian);
            }
            mqttService = Manager.GetService<MqttService>();
            Logger.Log("Start", "host", deviceHostName + ":" + modbusNodeId);
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
            if (!data.HasValue && !lastData.HasValue)
            {
                return;
            }
            if (data.HasValue && lastData.HasValue && Math.Abs(data.Value - lastData.Value) < double.Epsilon)
            {
                return;
            }
            lastData = data;

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
