using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoRefundOtherSports.Services;

public interface IMatchService
{
    Task<List<Match>> GetMatches();

    Task Update(Match match, string remark, int status);

    Task Push(Match match);

    Task CheckMissedMatch();
}

public class MatchServiceImp : IMatchService
{
    private readonly IDynamicRepository repository;

    private const string KeyLeagueId = "Settlement-AutoPayoutOtherSport-LeagueId";

    private readonly ICache cache;

    private readonly AppSettingsKey appSettingsKey;

    private readonly ICreateQueryService createQueryService;

    private readonly ILoggingService loggingService;

    private readonly SettleService settleService;

    private const int DeploymentError = 99;

    public MatchServiceImp(IDynamicRepository repository,
        ICache cache,
        IOptions<AppSettingsKey> appSettingsKey,
        ICreateQueryService createQueryService,
        ILoggingService loggingService,
        SettleService settleService)
    {
        this.repository = repository;
        this.cache = cache;
        this.appSettingsKey = appSettingsKey.Value;
        this.createQueryService = createQueryService;
        this.loggingService = loggingService;
        this.settleService = settleService;
    }

    public async Task<List<Match>> GetMatches()
    {
        var resultDetails = (await repository.FetchAsync<ResultDetail>(new ResultListingCriteria())).ToList();
        var matches = GroupMatch(resultDetails);

        cache.TryGet(KeyLeagueId, out int leagueId);

        return appSettingsKey.EnvironmentName switch
        {
            "PRO" => leagueId > 0
                                   ? (from m in matches where m.LeagueId != leagueId select m).ToList()
                                   : matches,
            "UAT" => (from m in matches where m.LeagueId == leagueId select m).ToList(),
            _ => matches,
        };
    }

    private static List<Match> GroupMatch(List<ResultDetail> resultDetails)
    {
        var matches = resultDetails.GroupBy(result => new { result.MatchId, result.LeagueId, result.TypeId })
            .Select(match => new Match
            {
                MatchId = match.Key.MatchId,
                LeagueId = match.Key.LeagueId,
                TypeId = match.Key.TypeId,
                ResultDetails = match.ToList()
            }).ToList();

        return matches;
    }

    public async Task Update(Match match, string remark, int status)
    {
        var criteria = createQueryService.QueryUpdateResult(match, remark, status);

        await repository.ExecuteAsync(criteria);
    }

    public async Task Push(Match match)
    {
        var result = await settleService.SettleMatch(match);
        if (result.ErrCode == DeploymentError)
        {
            await SetFlagDeploySpName();
        }
        else
        {
            await SettleHedgeMatchOrWriteLog(match, result);
        }
    }

    private async Task SettleHedgeMatchOrWriteLog(Match match, Result result)
    {
        if (result.IsSuccessful)
        {
            await SettleHedgeMatch(match);
        }
        else
        {
            await loggingService.WriteLog(match, result.ErrMsg);
            await Update(match, result.ErrMsg, MatchStatus.Failed);
        }
    }

    public async Task CheckMissedMatch()
    {
        var criteria = new UpdateResultStatusCriteria
        {
            StatusId = MatchStatus.Failed
        };
        await repository.ExecuteAsync(criteria);
    }

    private async Task SettleHedgeMatch(Match match)
    {
        var hedgeMatch = await GetHedgeMatch(match.MatchId);
        if (hedgeMatch != null)
        {
            match.MatchId = hedgeMatch.MatchId;
            var result = await settleService.SettleMatch(match);
            if (!result.IsSuccessful)
            {
                await loggingService.WriteLog(match, result.ErrMsg);
            }
        }
    }

    private async Task<HedgeMatchDetail> GetHedgeMatch(int matchId)
    {
        var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
        return await repository.GetAsync<HedgeMatchDetail>(criteria);
    }

    private async Task SetFlagDeploySpName()
    {
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(int.Parse(appSettingsKey.DeploymentDelay)));
        await cache.SetAsync(appSettingsKey.DeploymentSpName, 1, cachePolicy);
    }
}