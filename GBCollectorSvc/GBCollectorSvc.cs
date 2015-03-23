using GoodBet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using GoodBet.Collector;

namespace GBCollectorSvc
{
    public partial class GBCollectorSvc : ServiceBase
    {
        static string eventSource = "GB";
        static string eventLog = "GBLog";
        static int sleepInterval = 60;   // In secs

        public GBCollectorSvc()
        {
            InitializeComponent();
            GBCommon.ChangeToEventLog(eventSource, eventLog);
            GBCommon.DataFolder = @"c:\tests\GBData";
            if (!Directory.Exists(GBCommon.DataFolder))
            {
                Directory.CreateDirectory(GBCommon.DataFolder);
            }
        }

        protected override void OnStart(string[] args)
        {
            GBCommon.LogInfo("In service OnStart");
            while (true)
            {
                int startFrom = -1;
                int.TryParse(ConfigurationManager.AppSettings["startfrom"], out startFrom);
                if (startFrom < 0)
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

        protected override void OnStop()
        {
            GBCommon.LogInfo("In service OnStop");
        }
    }
}
