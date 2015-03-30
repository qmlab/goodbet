using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoodBet;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Goodbet.GBResult
{
    public class GBResultCollector
    {
        private Uri baseuri;

        public GBResultCollector(string baseuri)
        {
            this.baseuri = new Uri(baseuri);
        }

        public void UpdateResults()
        {
            var response1 = GBCommon.SendRequest(
                "{\"$or\":[{\"BetItem.Result\":{\"$not\":{\"$exists\":\"true\"}}}, {\"BetItem.Result\":\"null\"}]}, {\"BetItem.Result\":\"Unknown\"}]}",
                ConfigurationManager.AppSettings["datastore"],
                ConfigurationManager.AppSettings["apikey"],
                ConfigurationManager.AppSettings["passwd"],
                "application/json",
                "POST"
                );

            List<Stake> stakes = new List<Stake>();
            using (StreamReader streamReader = new StreamReader(response1.GetResponseStream()))
            {
                var text = streamReader.ReadToEnd();
                stakes = JsonConvert.DeserializeObject<List<Stake>>(text);
            }

            var matchesToQuery =
                from stake in stakes
                where stake.BetItem.Result == GameResult.Unknown
                select stake.BetItem.GameName;

            foreach (string matchIndex in matchesToQuery)
            {
                var result = this.QueryResultOfOneMatch(matchIndex);
                if (result != GameResult.Unknown)
                {
                    var response2 = GBCommon.SendRequest(
                        "[{\"BetItem.GameName\":\"" + matchIndex + "\"}, {" + "\"BetItem.Result\":\"" + result.ToString() + "\"}]",
                        ConfigurationManager.AppSettings["datastore"],
                        ConfigurationManager.AppSettings["apikey"],
                        ConfigurationManager.AppSettings["passwd"],
                        "application/json",
                        "PATCH"
                        );
                    using (StreamReader streamReader = new StreamReader(response2.GetResponseStream()))
                    {
                        var text = streamReader.ReadToEnd();
                        dynamic responseResult = JsonConvert.DeserializeObject(text);

                        if (responseResult.msg == "success")
                        {
                            GBCommon.LogInfo("Successfully updated result of {0} to {1}", matchIndex, result);
                        }
                        else
                        {
                            GBCommon.LogInfo("Failed to update result of {0}", matchIndex);
                        }
                    }
                }
            }
        }

        public GameResult QueryResultOfOneMatch(string matchIndex)
        {
            Uri uri = new Uri(baseuri, string.Format("Matches/{0}", matchIndex));
            WebRequest getRequest = WebRequest.Create(uri);

            using (Stream stream = getRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.Load(reader);

                    if (null == doc.DocumentNode)
                    {
                        throw new WebException(matchIndex + ": Error when parsing the root node");
                    }

                    HtmlNode bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                    if (null == bodyNode)
                    {
                        throw new WebException(matchIndex + ": Error when parsing the body node");
                    }

                    var scripts = bodyNode.SelectNodes("//script[@type='text/javascript']");
                    string pattern = @"var\s+matchHeaderJson\s*=\s*JSON.parse\(\'(.*)\'\);";
                    foreach (var script in scripts)
                    {
                        Match match = Regex.Match(script.InnerText, pattern);
                        if (match.Success)
                        {
                            string matchHeaderJson = match.Groups[1].Value;
                            if (!string.IsNullOrEmpty(matchHeaderJson))
                            {
                                dynamic matchHeader = JsonConvert.DeserializeObject(matchHeaderJson);
                                string resultStr = matchHeader.FullTimeResult;
                                if (!string.IsNullOrEmpty(resultStr))
                                {
                                    int score1 = int.Parse(resultStr.Split(':')[0].Trim(' '));
                                    int score2 = int.Parse(resultStr.Split(':')[1].Trim(' '));
                                    if (score1 < score2)
                                    {
                                        return GameResult.Lose;
                                    }
                                    else if (score1 == score2)
                                    {
                                        return GameResult.Draw;
                                    }
                                    else
                                    {
                                        return GameResult.Win;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return GameResult.Unknown;
        }
    }
}
