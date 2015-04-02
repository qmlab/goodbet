using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.IO;
using System.Configuration;

using GoodBet;
using System.Diagnostics;

namespace GoodBet.Collector
{
    public class GBCollector
    {
        private Uri baseuri;
        private GameType gameType;
        private TeamType teamType;
        private int toleranceCount;
        private static bool inProgress = false;
        private static object collectorLock = new object();

        public GBCollector(string baseuri, string gametype, string teamtype, string tolerantcount)
        {
            this.baseuri = new Uri(baseuri);
            this.gameType = (GameType)Enum.Parse(typeof(GameType), gametype);
            this.teamType = (TeamType)Enum.Parse(typeof(TeamType), teamtype);
            this.toleranceCount = Convert.ToInt32(tolerantcount);
        }

        protected void CollectOneMatch(string matchIndex)
        {
            Uri uri = new Uri(baseuri, string.Format("Matches/{0}/Betting", matchIndex));
            WebRequest getRequest = WebRequest.Create(uri);
            string matchAlias = gameType.ToString() + "-" + matchIndex;
            AveragedNonWeightedBetItemManager mgr = new AveragedNonWeightedBetItemManager();
            Guid matchGuid = Guid.NewGuid();
            string team1 = "", team2 = "", dateStr = "";

            using (Stream stream = getRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(reader);

                    if (null == doc.DocumentNode)
                    {
                        throw new WebException(matchAlias + ": Error when parsing the root node");
                    }

                    HtmlNode bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                    if (null == bodyNode)
                    {
                        throw new WebException(matchAlias + ": Error when parsing the body node");
                    }

                    var scripts = bodyNode.SelectNodes("//script[@type='text/javascript']");
                    string pattern = @"matchHeader.load.*\d+,\d+,\'([A-Za-z0-9\. ]+)\',\'([A-Za-z0-9\. ]+)\',\'([0-9:/ ]+)\'";
                    foreach (var script in scripts)
                    {
                        Match match = Regex.Match(script.InnerText, pattern);
                        if (match.Success)
                        {
                            team1 = match.Groups[1].Value.Replace(' ', '_');
                            team2 = match.Groups[2].Value.Replace(' ', '_');
                            dateStr = match.Groups[3].Value;
                        }
                    }
                    if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(team1) || string.IsNullOrEmpty(team2))
                    {
                        throw new WebException(matchAlias + ": No match time or team name is found");
                    }


                    var bookMakerNameNodes = bodyNode.SelectNodes("//div[@id='ThreeWay-OrdinaryTime']//a[@class='bm-name']");
                    if (null == bookMakerNameNodes)
                    {
                        throw new ApplicationException(matchAlias + ": No target odds found");
                    }
                    foreach (var bookMakerNameNode in bookMakerNameNodes)
                    {
                        var node = bookMakerNameNode.ParentNode.ParentNode;
                        string bookMaker = node.ChildNodes[1].SelectSingleNode(".//a[@class='bm-name']").InnerText;
                        string win = node.ChildNodes[3].SelectSingleNode(".//a/span").InnerText.Trim();
                        string draw = node.ChildNodes[5].SelectSingleNode(".//a/span").InnerText.Trim();
                        string lose = node.ChildNodes[7].SelectSingleNode(".//a/span").InnerText.Trim();
                        BetItem bet = new BetItem(
                            matchGuid.ToString(),
                            matchIndex,
                            gameType,
                            new System.Collections.Generic.List<Team>() { new Team(team1, teamType), new Team(team2, teamType) },
                            new ThreeWayOdds(Convert.ToDouble(win), Convert.ToDouble(lose), Convert.ToDouble(draw)),
                            false,
                            DateTime.UtcNow,
                            Convert.ToDateTime(dateStr),
                            bookMaker
                            );
                        mgr.CurrentBets.Add(bet);
                    }

                    string fileName = GBCommon.ConstructRecordFileName(gameType, team1, team2, dateStr);
                    if (!File.Exists(fileName))
                    {
                        mgr.Serialize(fileName);
                        GBCommon.LogInfo("{0}: Collected {1}", DateTime.Now, fileName);
                    }
                    else
                    {
                        GBCommon.LogInfo("{0}: {1} skipped", DateTime.Now, fileName);
                    }

                }
            }
        }

        public void Collect()
        {
            Collect(0);
        }

        /// <summary>
        /// Collect all available records from last index or a particular index
        /// </summary>
        /// <param name="startFrom">Specified starting index</param>
        public void Collect(int startFrom)
        {
            lock(collectorLock)
            {
                if (inProgress)
                {
                    GBCommon.LogInfo("{0}: Another collection in progress", DateTime.Now);
                    return;
                }
                else
                {
                    inProgress = true;
                }
            }

            try
            {
                int startIndex = startFrom > 0 ? startFrom : -99 + GBCommon.ReadContinuationIndex(GBCommon.ConstructCollectContinuationFileName(gameType));

                Stopwatch watch = new Stopwatch();
                watch.Start();
                int count = 0;
                int lastValid = -1;
                int currentIndex = startIndex;
                for (; ; currentIndex++)
                {
                    try
                    {
                        CollectOneMatch(currentIndex.ToString());
                        lastValid = currentIndex;

                        // Reset the count for valid index
                        count = 0;
                    }
                    catch (ApplicationException e)
                    {
                        if (++count < toleranceCount)
                        {
                            //GBCommon.LogInfo("Skip {0}", e.Message);
                        }
                        else
                        {
                            GBCommon.LogInfo("Stops at {0}", e.Message);
                            GBCommon.LogInfo("Elapsed time: {0}", watch.Elapsed);
                            break;
                        }
                    }
                    catch (WebException e)
                    {
                        GBCommon.LogInfo("Stops at {0}", e.Message);
                        GBCommon.LogInfo("Elapsed time: {0}", watch.Elapsed);
                        break;
                    }
                }

                if (lastValid >= startIndex)
                {
                    GBCommon.WriteContinuationIndex(GBCommon.ConstructCollectContinuationFileName(gameType), lastValid);
                }

                Console.WriteLine("Collection Completed.");
            }
            finally
            {
                lock(collectorLock)
                {
                    inProgress = false;
                }
            }
        }
    }
}
