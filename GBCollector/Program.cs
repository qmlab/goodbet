using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoodBet.Collector
{
    class Program
    {
        static void Main(string[] args)
        {
            int startFrom = -1;
            string gameType = string.Empty;
            if (!args.Any())
            {
                PrintUsage();
                return;
            }
            if (args.Any())
            {
                if (args[0].Contains("/?") || args[0].Contains("/h"))
                {
                    PrintUsage();
                    return;
                }
                else
                {
                    // Check the commandline arg first
                    gameType = args[0];
                    if (args.Count() >= 2)
                    {
                        int.TryParse(args[1], out startFrom);
                    }
                }
            }

            // Check the app config second
            if (startFrom <= 0)
            {
                int.TryParse(ConfigurationManager.AppSettings["startfrom"], out startFrom);
            }

            //GBCommon.DataFolder = @"c:\tests\GBData";
            if (!Directory.Exists(GBCommon.DataFolder))
            {
                Directory.CreateDirectory(GBCommon.DataFolder);
            }

            GBCollector collector = new GBCollector(
                ConfigurationManager.AppSettings["baseuri"], 
                gameType, 
                ConfigurationManager.AppSettings["teamtype"],
                ConfigurationManager.AppSettings["tolerancecount"]);

            int sleepInterval = 60;   // In secs
            while (true)
            {
                GBCommon.LogInfo("Starting a new collection round...");
                GBCommon.LogInfo("Current time: {0}", DateTime.Now);
                string dataFolder = ConfigurationManager.AppSettings["datafolder"];
                if (!string.IsNullOrEmpty(dataFolder) && !string.IsNullOrEmpty(gameType))
                {
                    GBCommon.DataFolder = dataFolder;
                    if (!Directory.Exists(Path.Combine(dataFolder, gameType)))
                    {
                        Directory.CreateDirectory(Path.Combine(dataFolder, gameType));
                    }
                }

                // Check the ini third
                if (startFrom <= 0)
                {
                    collector.Collect();
                }
                else
                {
                    collector.Collect(startFrom);
                    startFrom = 0;
                }

                int.TryParse(ConfigurationManager.AppSettings["collectinterval"], out sleepInterval);
                Thread.Sleep(TimeSpan.FromSeconds(sleepInterval));
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("Usage: {0} <PremierLeague|ChampionsLeague> [StartFromIndex]", AppDomain.CurrentDomain.FriendlyName);
        }
    }
}
