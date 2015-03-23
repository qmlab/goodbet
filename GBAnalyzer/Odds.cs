using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoodBet
{
    public interface IOdds
    {
        double Payout();
        OddsType Type { get; }
    }

    public enum OddsType
    {
        ThreeWay
    }

    /// <summary>
    /// Win, Lose or Draw
    /// </summary>
    [Serializable]
    public class ThreeWayOdds: IOdds
    {
        public double Win
        {
            get;
            set;
        }

        public double Lose
        {
            get;
            set;
        }

        public double Draw
        {
            get;
            set;
        }

        public ThreeWayOdds(double win, double lose, double draw)
        {
            Win = win;
            Lose = lose;
            Draw = draw;
        }

        public ThreeWayOdds()
        {
            Win = Lose = Draw = 0;
        }

        public double Payout()
        {
            try
            {
                return 1 / (1 / Win + 1 / Lose + 1 / Draw);
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Divided by Zero during payout calculation");
                Console.WriteLine("Win:{0}, Lose:{1}, Draw{2}", Win, Lose, Draw);
                return -1;
            }
        }

        private static OddsType type = OddsType.ThreeWay;

        public OddsType Type
        {
            get
            {
                return type;
            }
        }

    }
}
