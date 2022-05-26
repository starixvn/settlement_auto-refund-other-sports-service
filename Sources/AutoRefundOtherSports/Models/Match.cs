using System.Collections.Generic;

namespace AutoRefundOtherSports.Models
{
    public class Match
    {
        public int MatchId { get; set; }

        public int LeagueId { get; set; }

        public int TypeId { get; set; }

        public List<ResultDetail> ResultDetails { get; set; }

        public override bool Equals(object obj)
        {
            return obj != null
                   && (obj is Match)
                   && (obj as Match).MatchId == MatchId;
        }

        public override int GetHashCode()
        {
            return MatchId;
        }
    }
}