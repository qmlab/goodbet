using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace GoodBet.GBReport
{
    public class GBReporter
    {
        public static void Report()
        {

            foreach (string folder in Directory.GetDirectories(GBCommon.DataFolder))
            {
                GameType gameType = (GameType)Enum.Parse(typeof(GameType), folder.Remove(0, GBCommon.DataFolder.Length).TrimStart('\\'));
                DateTime continuationStartDate = GBCommon.ReadContinuationDate(GBCommon.ConstructReportContinuationFileName(gameType));
                DateTime latest = continuationStartDate;

                var files = new DirectoryInfo(folder).GetFiles("*.json", SearchOption.TopDirectoryOnly).Where(file => file.LastWriteTime > continuationStartDate + TimeSpan.FromSeconds(1));
                foreach (var file in files)
                {
                    AveragedNonWeightedBetItemManager mgr = new AveragedNonWeightedBetItemManager();
                    mgr.Deserialize(file.FullName, OddsType.ThreeWay);
                    if (mgr.CurrentBets.Count < 1)
                    {
                        throw new FileLoadException(string.Format("Record file {0} not valid", file.Name));
                    }
                    BetItem firstBet = mgr.CurrentBets[0];
                    Stake bestStake = mgr.BestStake(firstBet.GameName, firstBet.GameType, firstBet.Odds.Type);
                    string output = bestStake.ROI.ToString("p2")
                        + " "
                        + bestStake.BetItem.MatchDate.ToShortDateString()
                        + " "
                        + bestStake.BetItem.Teams[0].Name
                        + " "
                        + bestStake.BetItem.Teams[1].Name
                        + " "
                        + bestStake.BetItem.BookMaker.Replace(' ', '_')
                        + " "
                        + bestStake.BetItem.Odds.Type.ToString()
                        + " "
                        + bestStake.Decision;
                    GBCommon.Report(output, gameType);
                    if (file.LastWriteTime > latest)
                        latest = file.LastWriteTime;

                    // If the game stake exists, delete the old one
                    var deleteStakeResponse = GBCommon.SendRequest(
                        "{\"BetItem.GameName\": \"" + bestStake.BetItem.GameName + "\"}",
                        ConfigurationManager.AppSettings["datastore"],
                        ConfigurationManager.AppSettings["apikey"],
                        ConfigurationManager.AppSettings["passwd"],
                        "application/json",
                        "DELETE"
                        );
                    using (StreamReader streamReader = new StreamReader(deleteStakeResponse.GetResponseStream()))
                    {
                        var text = streamReader.ReadToEnd();
                        Console.WriteLine(text);
                    }

                    // Add the new stake
                    var addStakeResponse = GBCommon.SendRequest(
                        JsonConvert.SerializeObject(bestStake),
                        ConfigurationManager.AppSettings["datastore"],
                        ConfigurationManager.AppSettings["apikey"],
                        ConfigurationManager.AppSettings["passwd"],
                        "application/json",
                        "PUT"
                        );
                    using (StreamReader streamReader = new StreamReader(addStakeResponse.GetResponseStream()))
                    {
                        var text = streamReader.ReadToEnd();
                        Console.WriteLine(text);
                    }
                }

                if (latest > continuationStartDate)
                {
                    GBCommon.WriteContinuationDate(GBCommon.ConstructReportContinuationFileName(gameType), latest);
                }
            }

            GBCommon.LogInfo("Reporting Completed.");
        }
    }
}
