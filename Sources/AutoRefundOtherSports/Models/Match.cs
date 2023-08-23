using System.Text.RegularExpressions;

namespace AutoRefundOtherSports.Models;

public class Match
{
    public int MatchId { get; set; }

    public int LeagueId { get; set; }

    public int TypeId { get; set; }

    public int SportType { get; set; }

    public int ParentId { get; set; }

    public List<ResultDetail> ResultDetails { get; set; }
    public int TryCount { get; set; }

    public bool IsExistsGameWinner => ResultDetails.Exists(x => x.SourceId == SourceIds.TennisGameWinner);

    public override bool Equals(object obj) =>
        obj is Match match
        && match.MatchId == MatchId;

    public override int GetHashCode() => MatchId;
}