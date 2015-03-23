using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GoodBet
{


    [Serializable]
    public class BetItem
    {
        #region Properties
        public string Id
        {
            get;
            set;
        }

        public string GameName
        {
            get;
            set;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public GameType GameType
        {
            get;
            set;
        }

        public List<Team> Teams
        {
            get;
            set;
        }

        public IOdds Odds
        {
            get;
            set;
        }

        public bool Aggregated
        {
            get;
            set;
        }

        public DateTime OddsDate
        {
            get;
            set;
        }

        public DateTime MatchDate
        {
            get;
            set;
        }

        public string BookMaker
        {
            get;
            set;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public GameResult Result
        {
            get;
            set;
        }
        #endregion


        #region Methods
        public BetItem(string id, 
            string gameName,
            GameType gameType,
            List<Team> teams, 
            ThreeWayOdds odds, 
            bool aggregated, 
            DateTime oddsDate, 
            DateTime matchDate,
            string bookMaker)
        {
            Init(id, gameName, gameType, teams, aggregated, oddsDate, matchDate, bookMaker);
            Odds = odds;
        }

        private void Init(string id, string gameName, GameType gameType, List<Team> teams, bool aggregated, DateTime oddsDate,
            DateTime matchDate, string bookMaker)
        {
            Id = id;
            GameName = gameName;
            GameType = gameType;
            Teams = new List<Team>();
            foreach (var team in teams)
            {
                Teams.Add(team);
            }
            Aggregated = aggregated;
            OddsDate = oddsDate;
            MatchDate = matchDate;
            BookMaker = bookMaker;
        }

        public bool IsHistorical()
        {
            return OddsDate < MatchDate;
        }
        #endregion

    }

    [Serializable]
    public class Team
    {
        public string Name
        {
            get;
            set;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public TeamType Type
        {
            get;
            set;
        }

        public Team(string name, TeamType type)
        {
            Name = name;
            Type = type;
        }
    }
}
