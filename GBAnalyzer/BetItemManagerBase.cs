using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;

namespace GoodBet
{
    public abstract class BetItemManagerBase
    {
        public List<BetItem> CurrentBets;

        public BetItemManagerBase()
        {
            CurrentBets = new List<BetItem>();
        }

        public bool Serialize(string fileName)
        {
            return GBCommon.Serialize(this.CurrentBets, fileName);
        }

        public bool Deserialize(string fileName, OddsType oddsType)
        {
            return GBCommon.Deserialize(out this.CurrentBets, fileName, oddsType);
        }

        public IOdds AverageOdds(string gameName, GameType gameType, OddsType oddsType)
        {
            try
            {
                switch (oddsType)
                {
                    case OddsType.ThreeWay:
                        ThreeWayOdds avgOdds = new ThreeWayOdds();
                        int count = 0;
                        foreach (BetItem item in CurrentBets)
                        {
                            if (item.GameName.Equals(gameName, StringComparison.InvariantCultureIgnoreCase)
                                && item.GameType == gameType
                                && oddsType == OddsType.ThreeWay)
                            {
                                avgOdds.Win += ((ThreeWayOdds)(item.Odds)).Win;
                                avgOdds.Lose += ((ThreeWayOdds)(item.Odds)).Lose;
                                avgOdds.Draw += ((ThreeWayOdds)(item.Odds)).Draw;
                                count++;
                            }
                        }
                        avgOdds.Win /= count;
                        avgOdds.Lose /= count;
                        avgOdds.Draw /= count;
                        return avgOdds;
                    default:
                        break;
                }
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Not enough sample size during average odds calculation");
                Console.WriteLine("GameName:{0}, GameType:{1}, OddesType:{2}", gameName, gameType, oddsType.ToString());
            }
            return null;
        }

        protected List<Stake> MaxOddsStakes(string gameName, GameType gameType, OddsType oddsType)
        {
            switch (oddsType)
            {
                case OddsType.ThreeWay:
                    Stake [] maxOddsStakes = new Stake[3];
                    foreach (BetItem item in CurrentBets)
                    {
                        if (item.GameName.Equals(gameName, StringComparison.InvariantCultureIgnoreCase)
                            && item.GameType == gameType
                            && oddsType == OddsType.ThreeWay)
                        {
                            ThreeWayOdds itemOdds = (ThreeWayOdds)(item.Odds);
                            if (null == maxOddsStakes[0] || itemOdds.Win > ((ThreeWayOdds)maxOddsStakes[0].BetItem.Odds).Win)
                            {
                                maxOddsStakes[0] = new Stake(item, "Win");
                            }
                            if (null == maxOddsStakes[1] || itemOdds.Lose > ((ThreeWayOdds)maxOddsStakes[1].BetItem.Odds).Lose)
                            {
                                maxOddsStakes[1] = new Stake(item, "Lose");
                            }
                            if (null == maxOddsStakes[2] || itemOdds.Draw > ((ThreeWayOdds)maxOddsStakes[2].BetItem.Odds).Draw)
                            {
                                maxOddsStakes[2] = new Stake(item, "Draw");
                            }
                        }
                    }
                    return maxOddsStakes.ToList();
                default:
                    break;
            }

            return null;
        }

        public abstract IOdds TrueOdds(IOdds averageOdds);

        public abstract Stake BestStake(string gameName, GameType gameType, OddsType oddsType);
    }

}
