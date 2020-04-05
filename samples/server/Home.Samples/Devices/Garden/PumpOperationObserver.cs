using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    class PumpOperationObserver
    {
        private FileInfo _pumpFile;
        private DigitalInputArraySink _pumpSink;
        private int _pumpSubIndex;

        public PumpOperationObserver()
        {
            var dbFolder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/GARDEN"));
            _pumpFile = new FileInfo(Path.Combine(dbFolder.FullName, "pump.log"));
            Log("{0:u} Started", DateTime.Now);
        }

        public void OnSinkRemoved(SubSink removed)
        {
            if (_pumpSink != null && removed.Sink == _pumpSink && removed.SubIndex == _pumpSubIndex)
            {
                Log("{0:u} Sink Unregistered: {1}", DateTime.Now, removed);
                _pumpSink.EventReceived -= HandlePumpSinkData;
                _pumpSink = null;
            }
        }

        public void OnSinkAdded(SubSink added)
        {
            var diar = added.Sink as DigitalInputArraySink;
            if (diar != null)
            {
                Log("{0:u} Sink Registered: {1}", DateTime.Now, added);

                if (_pumpSink != null)
                {
                    _pumpSink.EventReceived -= HandlePumpSinkData;
                }

                _pumpSink = diar;
                _pumpSubIndex = added.SubIndex;
                _pumpSink.EventReceived += HandlePumpSinkData;
            }
        }

        private void HandlePumpSinkData(object sender, DigitalInputArraySink.EventReceivedEventArgs e)
        {
            if (e.SubIndex == _pumpSubIndex)
            {
                // Log change
                // 220v sense is inverted bool
                Log("{0:u} Pump {1}", e.Timestamp, e.State ? "OFF" : "ON");
            }
        }

        private void Log(string format, params object[] args)
        {
            using (var writer = new StreamWriter(_pumpFile.FullName, true))
            {
                writer.WriteLine(format, args);
            }
        }
    }
}
