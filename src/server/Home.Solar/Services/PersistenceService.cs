using System;
using System.IO;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Helper service for persistence
    /// </summary>
    public class PersistenceService : ServiceBase
    {
        public string GetAppFolderPath(string root = null)
        {
            string wrkDir = Manager.GetService<IConfigurationService>().GetConfig("wrk");
            if (wrkDir == null)
            {
                wrkDir = Path.Combine(Environment.CurrentDirectory, "etc");
            }
            if (!string.IsNullOrEmpty(root))
            {
                wrkDir = Path.Combine(wrkDir, root);
            }
            wrkDir = Path.GetFullPath(wrkDir);
            if (!Directory.Exists(wrkDir))
            {
                Directory.CreateDirectory(wrkDir);
            }
            return wrkDir;
        }
    }
}
