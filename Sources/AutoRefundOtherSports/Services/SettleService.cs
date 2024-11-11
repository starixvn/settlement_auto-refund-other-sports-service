using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoRefundOtherSports.Services;

public interface SettleService
{
    Task SettleMatch(Match match);
}

public class SettleServiceImp(
    CreateQueryService createQueryService,
    IDynamicRepository repository,
    ScaleOutSettleService scaleOutSettleService,
    RetryValidationService retryValidationService,
    LoggingService loggingService,
    IOptions<AppSettingsKey> appSettingsKey)
    : SettleService
{
    public async Task SettleMatch(Match match)
    {
        var criteria = createQueryService.QuerySettle(match);

        var result = match.IsExistsGameWinner
            ? await ValidateBeforeExecuteForGameWinner(criteria)
            : await ValidateBeforeExecute(criteria);

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

            var message = GenerateLogMessage(criteria);
            loggingService.WriteLog(criteria.MatchId, message);
            loggingService.InsertResettleLog(criteria, match.SportType);

            await ExecuteAsync(criteria);
            await loggingService.Update(match, string.Empty, MatchStatus.Completed);

            await SettleHedgeMatch(match, criteria);

            await HandleRefundGameWinner(match, result);
        }
        else
        {
            await loggingService.WriteLog(match, result.ErrMsg);
            await loggingService.Update(match, result.ErrMsg, MatchStatus.Failed);
        }
    }

    private async Task HandleRefundGameWinner(Match match, Result result)
    {
        if (result.ErrCode == 0 && match.SportType == SportType.Tennis && match.ParentId == 0)
        {
            await InsRefundResult(match);
        }
    }

    private async Task InsRefundResult(Match match)
    {
        var criteria = new InsRefundResultCriteria
        {
            MatchId = match.MatchId,
            ResourceIds = string.Join(',', match.ResultDetails.Select(r => r.ResourceId).Distinct())
        };

        await repository.ExecuteAsync(criteria);
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

    private async Task<Result> ValidateBeforeExecuteForGameWinner(SettleCriteria criteria)
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

        var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);

        return new Result
        {
            ErrCode = resultMessage.ErrorCode,
            ErrMsg = resultMessage.ErrorMessage
        };
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

    private static string GenerateLogMessage(SettleCriteria criteria)
        => $"NAP: Auto Payout Calculate {criteria.Action} for matchId: {criteria.MatchId} Result: {criteria.SelectedResources}";

    private async Task ExecuteAsync(SettleCriteria criteria)
    {
        var servers = appSettingsKey.Value.SiteDbServer;
        var tasks = servers.Select(server => ExecuteOneServer(criteria, server)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task ExecuteOneServer(SettleCriteria criteria, string server)
    {
        criteria.DbServer = server;
        await repository.ExecuteAsync(criteria);
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

            await loggingService.UpdateMatchStatus(new UpdateResultStatusCriteria
            {
                MatchId = hedgeMatch.MatchId,
                StatusId = MatchStatus.Completed,
                TypeId = match.TypeId,
                ResourceIds = string.Join(",", match.ResultDetails.Select(item => item.ResourceId).ToList()),
            });
        }
    }

    private async Task<HedgeMatchDetail> GetHedgeMatch(int matchId)
    {
        var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
        return await repository.GetAsync<HedgeMatchDetail>(criteria);
    }
}