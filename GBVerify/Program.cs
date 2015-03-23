using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodBet;
using Newtonsoft.Json;

namespace GBVerify
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "/?")
            {
                Usage();
                return;
            }

            double threshold = 0;

            if (args.Length >= 1)
            {
                string thresholdString = args[0];
                if (!double.TryParse(thresholdString, out threshold))
                {
                    Console.WriteLine("Invalid threshold");
                    return;
                }
            }

            var response = GBCommon.SendRequest(
                "{\"$and\":[{\"BetItem.Result\":{\"$exists\":\"true\"}}, {\"BetItem.Result\":{\"$ne\":\"Unknown\"}}, {\"ROI\": {\"$gte\":" + (threshold / 100).ToString() + "}}]}",
                ConfigurationManager.AppSettings["datastore"],
                ConfigurationManager.AppSettings["apikey"],
                ConfigurationManager.AppSettings["passwd"],
                "application/json",
                "POST"
                );

            List<Stake> stakes = new List<Stake>();
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                string responseString = streamReader.ReadToEnd();
                stakes = JsonConvert.DeserializeObject < List < Stake > > (responseString);
            }

            double actualBet = 0;
            double actualReturn = 0;
            foreach (var stake in stakes)
            {
                actualBet++;
                if (stake.Decision.ToString() == stake.BetItem.Result.ToString())
                {
                    if (stake.BetItem.Odds != null && stake.BetItem.Odds is ThreeWayOdds)
                    {
                        if (stake.BetItem.Result == GameResult.Win)
                        {
                            actualReturn += ((ThreeWayOdds) stake.BetItem.Odds).Win;
                        }
                        if (stake.BetItem.Result == GameResult.Draw)
                        {
                            actualReturn += ((ThreeWayOdds)stake.BetItem.Odds).Draw;
                        }
                        if (stake.BetItem.Result == GameResult.Lose)
                        {
                            actualReturn += ((ThreeWayOdds)stake.BetItem.Odds).Lose;
                        }
                    }
                }
            }

            if (actualBet == 0)
            {
                Console.WriteLine("N/A");
            }
            else
            {
                Console.WriteLine(actualReturn / actualBet);
            }
        }

        static void Usage()
        {
            Console.WriteLine("Usage: {0} [ROI Threshold in %]", AppDomain.CurrentDomain.FriendlyName);
        }
    }
}
