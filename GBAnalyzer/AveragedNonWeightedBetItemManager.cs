using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoodBet
{
    public class AveragedNonWeightedBetItemManager: BetItemManagerBase
    {
        public override IOdds TrueOdds(IOdds averageOdds)
        {
            try
            {
                switch (averageOdds.Type)
                {
                    case OddsType.ThreeWay:
                        ThreeWayOdds trueOdds = new ThreeWayOdds();
                        var curOdds = (ThreeWayOdds)averageOdds;
                        double payout = curOdds.Payout();
                        trueOdds.Win = curOdds.Win / payout;
                        trueOdds.Lose = curOdds.Lose / payout;
                        trueOdds.Draw = curOdds.Draw / payout;
                        return trueOdds;
                    default:
                        break;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Null reference to averageOdds in TrueOdds calculation");
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Divided by Zero in TrueOdds calculation");
            }
            return null;
        }

        public override Stake BestStake(string gameName, GameType gameType, OddsType oddsType)
        {
            try
            {
                IOdds avgOdds = AverageOdds(gameName, gameType, oddsType);
                IOdds trueOdds = TrueOdds(avgOdds);
                var maxOddsStakes = MaxOddsStakes(gameName, gameType, oddsType);
                Stake bestBet = null;
                switch (oddsType)
                {
                    case OddsType.ThreeWay:
                        foreach (Stake stake in maxOddsStakes)
                        {
                            double curRoi = 0;
                            if (stake.Decision.Equals("Win", StringComparison.InvariantCultureIgnoreCase))
                            {
                                curRoi = (((ThreeWayOdds)stake.BetItem.Odds).Win - ((ThreeWayOdds)trueOdds).Win) / ((ThreeWayOdds)trueOdds).Win;
                            }
                            else if (stake.Decision.Equals("Lose", StringComparison.InvariantCultureIgnoreCase))
                            {
                                curRoi = (((ThreeWayOdds)stake.BetItem.Odds).Lose - ((ThreeWayOdds)trueOdds).Lose) / ((ThreeWayOdds)trueOdds).Lose;
                            }
                            if (stake.Decision.Equals("Draw", StringComparison.InvariantCultureIgnoreCase))
                            {
                                curRoi = (((ThreeWayOdds)stake.BetItem.Odds).Draw - ((ThreeWayOdds)trueOdds).Draw) / ((ThreeWayOdds)trueOdds).Draw;
                            }
                            if (null == bestBet || curRoi > bestBet.ROI)
                            {
                                stake.ROI = curRoi;
                                bestBet = stake;
                            }
                        }
                        break;
                    default:
                        break;
                }
                return bestBet;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return null;
        }
    }
}
