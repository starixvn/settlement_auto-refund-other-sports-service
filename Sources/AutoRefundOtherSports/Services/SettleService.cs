using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoRefundOtherSports.Services;

public interface SettleService
{
    Task SettleMatch(Match match);
}

public class SettleServiceImp : SettleService
{
    private readonly CreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly RetryValidationService retryValidationService;

    private readonly LoggingService loggingService;

    private readonly AppSettingsKey appSettingsKey;

    private readonly LogDirectlyService logDirectlyService;


    public SettleServiceImp(CreateQueryService createQueryService,
        IDynamicRepository repository,
        ScaleOutSettleService scaleOutSettleService,
        RetryValidationService retryValidationService,
        LoggingService loggingService,
        IOptions<AppSettingsKey> appSettingsKey,
        LogDirectlyService logDirectlyService)
    {
        this.createQueryService = createQueryService;
        this.repository = repository;
        this.scaleOutSettleService = scaleOutSettleService;
        this.retryValidationService = retryValidationService;
        this.loggingService = loggingService;
        this.logDirectlyService = logDirectlyService;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task SettleMatch(Match match)
    {
        var criteria = createQueryService.QuerySettle(match);
        var result = await ValidateBeforeExecute(criteria);

        if (result.ErrCode != ErrorCode.DeploymentError)
        {
            await ProcessSettleMatch(result, match, criteria);
        }
    }

    private async Task ProcessSettleMatch(Result result, Match match, SettleCriteria criteria)
    {
        if (result.ErrCode == 0)
        {
            await loggingService.Update(match, string.Empty, MatchStatus.Running);
            await UpdateMatchInfo(criteria);

            logDirectlyService.WriteLog(criteria.MatchId, GenerateLogMessage(criteria));

            await ExecuteAsync(criteria);
            await SettleHedgeMatch(match, criteria);
        }
        else
        {
            await loggingService.WriteLog(match, result.ErrMsg);
            await loggingService.Update(match, result.ErrMsg, MatchStatus.Failed);
        }
    }

    private async Task<Result> ValidateBeforeExecute(SettleCriteria criteria)
    {
        var request = new EventTicketRequest
        {
            MatchId = criteria.MatchId,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Mode = "ALL",
            SelectedResources = criteria.SelectedResources,
            IsAuto = true
        };
        return await retryValidationService.ValidateEventTicket(request);
    }

    private async Task UpdateMatchInfo(SettleCriteria criteria)
    {
        var requestUpdateInfo = new UpdateMatchInfoRequest
        {
            Action = criteria.Action,
            AdminId = criteria.AdminId,
            MatchId = criteria.MatchId,
            SelectedResources = criteria.SelectedResources,
            IsAuto = criteria.IsAuto,
        };

        await scaleOutSettleService.UpdateMatchInfo(requestUpdateInfo);
    }

    private string GenerateLogMessage(SettleCriteria criteria)
        => $"NAP: Auto Payout Calculate {criteria.Action} for matchId: {criteria.MatchId} Result: {criteria.SelectedResources}";

    private async Task ExecuteAsync(SettleCriteria criteria)
    {
        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task SettleHedgeMatch(Match match, SettleCriteria criteria)
    {
        var hedgeMatch = await GetHedgeMatch(match.MatchId);
        if (hedgeMatch != null)
        {
            criteria.MatchId = hedgeMatch.MatchId;
            criteria.BroadcastSessionId = Guid.NewGuid().ToString();
            await UpdateMatchInfo(criteria);
            await ExecuteAsync(criteria);
        }
    }

    private async Task<HedgeMatchDetail> GetHedgeMatch(int matchId)
    {
        var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
        return await repository.GetAsync<HedgeMatchDetail>(criteria);
    }
}