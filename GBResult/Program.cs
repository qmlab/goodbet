using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoodBet;
using Goodbet.GBResult;
using Newtonsoft.Json;

namespace GBResult
{
    class Program
    {
        static void Main(string[] args)
        {
            GBResultCollector collector = new GBResultCollector(ConfigurationManager.AppSettings["baseuri"]);

            int sleepInterval = 3600;   // Default value in secs
            while (true)
            {
                GBCommon.LogInfo("Starting a new result query round...");
                GBCommon.LogInfo("Current time: {0}", DateTime.Now);

                collector.UpdateResults();

                int.TryParse(ConfigurationManager.AppSettings["collectinterval"], out sleepInterval);
                Thread.Sleep(TimeSpan.FromSeconds(sleepInterval));
            }
        }
    }
}
