using AutoRefundOtherSports.Exceptions;

namespace AutoRefundOtherSports.Services;

public interface MatchService
{
    Task<List<Match>> GetMatches();

    Task CheckMissedMatch();

    Task HandleAutoRefundException(Match match, AutoRefundException exception);
}

public class MatchServiceImp : MatchService
{
    private readonly IDynamicRepository repository;

    private readonly ICache cache;

    private readonly AppSettingsKey appSettingsKey;

    private readonly LoggingService loggingService;

    private readonly List<int> listSport = new() { SportType.Volleyball, SportType.Badminton, SportType.Tennis, SportType.ESports, SportType.TableTennis };

    public MatchServiceImp(IDynamicRepository repository,
        ICache cache,
        IOptions<AppSettingsKey> appSettingsKey,
        LoggingService loggingService)
    {
        this.repository = repository;
        this.cache = cache;
        this.appSettingsKey = appSettingsKey.Value;
        this.loggingService = loggingService;
    }

    public async Task<List<Match>> GetMatches()
    {
        var resultDetails = await GetResultDetail();
        var matches = GroupMatch(resultDetails);

        var (_, response) = await cache.TryGetAsync<string>(CacheKey.LeagueId);

        var leagueIds = (response ?? "").Split(",");

        return appSettingsKey.EnvironmentName switch
        {
            "PRO" => leagueIds.Length > 0
                ? matches.Where(m => !leagueIds.Contains(m.LeagueId.ToString())).ToList()
                : matches,
            "UAT" => matches.Where(m => leagueIds.Contains(m.LeagueId.ToString())).ToList(),
            _ => matches
        };
    }

    private async Task<IEnumerable<ResultDetail>> GetResultDetail()
    {
        var resultDetails = new List<ResultDetail>();

        foreach (var sportType in listSport)
        {
            var resultDetail = (await repository.FetchAsync<ResultDetail>(new ResultListingCriteria { SportType = sportType })).ToList();
            resultDetails.AddRange(resultDetail);
        }

        return resultDetails;
    }

    private static List<Match> GroupMatch(IEnumerable<ResultDetail> resultDetails)
    {
        var matches = resultDetails.GroupBy(result => new { result.MatchId, result.LeagueId, result.TypeId, result.SportType, result.ParentId })
            .Select(match => new Match
            {
                MatchId = match.Key.MatchId,
                LeagueId = match.Key.LeagueId,
                TypeId = match.Key.TypeId,
                SportType = match.Key.SportType,
                ParentId = match.Key.ParentId,
                ResultDetails = match.ToList()
            }).ToList();

        return matches;
    }

    public async Task CheckMissedMatch()
    {
        var criteria = new UpdateResultStatusCriteria
        {
            StatusId = MatchStatus.Failed
        };
        await repository.ExecuteAsync(criteria);
    }

    public async Task HandleAutoRefundException(Match match, AutoRefundException exception)
    {
        await loggingService.WriteLog(match, exception);
        await loggingService.Update(match, exception.Description, MatchStatus.Failed);
    }
}