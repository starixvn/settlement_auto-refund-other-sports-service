namespace AutoRefundOtherSports.Services;

public interface MatchPoolService
{
    Task<List<Match>> PeekNewMatch();

    void RemoveMatch(Match match);
}

public class MatchPoolServiceImp(MatchService matchService) : MatchPoolService
{
    private readonly object lockObject = new();

    public List<Match> PoolOfMatches { get; } = [];

    public void RemoveMatch(Match match)
    {
        lock (lockObject)
        {
            PoolOfMatches.RemoveAll(m => m.MatchId == match.MatchId);
        }
    }

    public async Task<List<Match>> PeekNewMatch()
    {
        var matches = await matchService.GetMatches();

        lock (lockObject)
        {
            var newMatches = matches.Except(PoolOfMatches).ToList();
            PoolOfMatches.AddRange(newMatches);

            return newMatches;
        }
    }
}