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


                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["datastore"]);
                    request.KeepAlive = false;
                    request.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["apikey"], ConfigurationManager.AppSettings["passwd"]);
                    request.Method = "PUT";
                    request.ContentType = "application/json";
                    using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(bestStake);
                        streamWriter.Write(json);
                    }
                    request.GetRequestStream().Close();
                    var response = (HttpWebResponse)request.GetResponse();
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
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
