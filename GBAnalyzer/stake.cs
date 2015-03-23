using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoodBet
{
    public class Stake
    {
        public BetItem BetItem
        {
            get;
            set;
        }

        public string Decision
        {
            get;
            set;
        }

        public double ROI
        {
            get;
            set;
        }

        public Stake(BetItem betItem, string decision)
        {
            BetItem = betItem;
            Decision = decision;
        }

    }
}
