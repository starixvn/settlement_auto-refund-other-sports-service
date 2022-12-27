using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoRefundOtherSports.Services;

public interface SettleService
{
    Task<Result> SettleMatch(Match match);
}

public class SettleServiceImp : SettleService
{
    private readonly CreateQueryService createQueryService;

    private readonly IDynamicRepository repository;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly AppSettingsKey appSettingsKey;

    public SettleServiceImp(CreateQueryService createQueryService,
        IDynamicRepository repository,
        ScaleOutSettleService scaleOutSettleService,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.createQueryService = createQueryService;
        this.repository = repository;
        this.scaleOutSettleService = scaleOutSettleService;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<Result> SettleMatch(Match match)
    {
        var criteria = createQueryService.QuerySettle(match);
        var validateTicket = await ValidateBeforeExecute(criteria);
        if (validateTicket.ErrCode == 0)
        {
            await UpdateMatchInfo(criteria);
            await ExecuteAsync(criteria);
        }
        return validateTicket;
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
        for (var i = 0; i < appSettingsKey.RetryValidateCount; i++)
        {
            var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);
            if (resultMessage.ErrorCode == ErrorCode.MatchInQueue && i < appSettingsKey.RetryValidateCount - 1)
            {
                await Task.Delay(appSettingsKey.RetryValidate);
                continue;
            }
            return new Result
            {
                ErrCode = resultMessage.ErrorCode,
                ErrMsg = resultMessage.ErrorMessage
            };
        }

        return new Result();
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

    private async Task ExecuteAsync(SettleCriteria criteria)
    {
        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
        }
    }
}