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
        private bool lastDataValid = false;
        private readonly ILogger Logger;

        public ModbusAmmeter(string deviceHostName, int modbusNodeId)
        {
            this.modbusNodeId = modbusNodeId;
            Logger = Manager.GetService<LoggerFactory>().Create("Ammeter");

            if (deviceHostName != "")
            {
                modbusClient = Manager.GetService<ModbusClientFactory>().Get(deviceHostName, FluentModbus.ModbusEndianness.LittleEndian);
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

        /// <summary>
        /// Get both channel values, as ampere
        /// </summary>
        private async Task<float[]> GetData()
        {
            // PIC ammeter specs, little endian
            var buffer = await modbusClient.ReadHoldingRegistries(modbusNodeId, 0x200, 4);
            float[] ret = new float[2];
            for (int i = 0; i < 2; i++)
            {
                float value = ((uint)(buffer[i * 2] + (buffer[i * 2 + 1] << 16))) / 65536f;
                // full-scale = 1024, 50A sensor
                ret[i] = value / 1024f * 50f;
            }                        
            return ret;
        }

        private async Task PublishData(float[] data)
        {
            if (data == null && !lastDataValid)
            {
                return;
            }
            if (data != null)
            {
                await mqttService.RawPublish(AnalogIntegrator.HomeDataTopicId, Encoding.UTF8.GetBytes(data[0].ToString()));
                await mqttService.RawPublish(AnalogIntegrator.ImmissionDataTopicId, Encoding.UTF8.GetBytes(data[1].ToString()));
                lastDataValid = true;
            }
            else
            {
                await mqttService.RawPublish(AnalogIntegrator.HomeDataTopicId, new byte[0]);
                await mqttService.RawPublish(AnalogIntegrator.ImmissionDataTopicId, new byte[0]);
                lastDataValid = false;
            }
        }
    }
}
