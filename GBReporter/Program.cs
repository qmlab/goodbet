using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace GoodBet.GBReport
{
    class Program
    {
        static void Main(string[] args)
        {
            int sleepInterval = 60;   // In secs
            while (true)
            {
                GBCommon.LogInfo("Starting a new reporting round...");
                GBCommon.LogInfo("Current time: {0}", DateTime.Now);

                // Dynamically change data folder
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["datafolder"]))
                {
                    GBCommon.DataFolder = ConfigurationManager.AppSettings["datafolder"];
                    if (!Directory.Exists(GBCommon.DataFolder))
                    {
                        Directory.CreateDirectory(GBCommon.DataFolder);
                    }
                }

                GBReporter.Report();

                int.TryParse(ConfigurationManager.AppSettings["reportinterval"], out sleepInterval);
                Thread.Sleep(TimeSpan.FromSeconds(sleepInterval));
            }
        }


    }
}
