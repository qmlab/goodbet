using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.IO;
using System.Configuration;
using System.Threading;

namespace GoodBet.Collector
{
    [ServiceContract]
    public interface IGBCollectorService1
    {
        void CrawWhoScored();
    }

    public class GBCollectorService1 : IGBCollectorService1
    {
        static string eventSource = "GB";
        static string eventLog = "GBLog";
        static int sleepInterval = 60;   // In secs
        static bool isRunning = false;

        public void CrawWhoScored()
        {
            if (isRunning)
                return;

            GBCommon.DataFolder = @"c:\tests\GBData";
            if (!Directory.Exists(GBCommon.DataFolder))
            {
                Directory.CreateDirectory(GBCommon.DataFolder);
            }

            while (true)
            {
                isRunning = true;
                GBCommon.LogInfo("Starting a new round...");
                int startFrom = -1;
                int.TryParse(ConfigurationManager.AppSettings["startfrom"], out startFrom);
                if (startFrom <= 0)
                {
                    GBCollector.Collect();
                }
                else
                {
                    GBCollector.Collect(startFrom);
                }

                int.TryParse(ConfigurationManager.AppSettings["collectinterval"], out sleepInterval);
                Thread.Sleep(TimeSpan.FromSeconds(sleepInterval));
            }
        }
    }
}
