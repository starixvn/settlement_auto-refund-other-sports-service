using AutoRefundOtherSports.Exceptions;
using Fanex.Logging;

namespace AutoRefundOtherSports.Services;

public interface LoggingService
{
    Task WriteLog(Match match, string message);

    Task WriteLog(int matchId, string message);

    Task WriteLog(Match match, AutoRefundException exception);

    Task Update(Match match, string remark, int status);

    Task UpdateMatchStatus(UpdateResultStatusCriteria criteria);

    Task InsertResettleLog(SettleCriteria criteria, int sportType);
}

public class LoggingServiceImp(IDynamicRepository repository, CreateQueryService createQueryService)
    : LoggingService
{
    public async Task WriteLog(Match match, string message)
    {
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match, errorMessage);
        await repository.ExecuteAsync(criteria);
    }

    public async Task WriteLog(Match match, AutoRefundException exception)
    {
        var message = exception.Description ?? "Auto Refund failed due to system error.";
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match, errorMessage);
        await repository.ExecuteAsync(criteria);
    }

    public async Task Update(Match match, string remark, int status)
    {
        var criteria = createQueryService.QueryUpdateResult(match, remark, status);

        await repository.ExecuteAsync(criteria);
    }

    public async Task UpdateMatchStatus(UpdateResultStatusCriteria criteria)
    {
        await repository.ExecuteAsync(criteria);
    }

    private static string GenerateErrorLogMessage(Match match, string message)
    {
        var resourceIds = match.ResultDetails.Select(resultDetail => resultDetail.ResourceId).ToList();
        var resources = string.Join(",", resourceIds);
        var title = $"NAP: Auto Refund OS Result: {resources}";
        var remark = new List<string>
            {
                title, message
            };

        return string.Join("<br/>", remark);
    }

    public async Task InsertResettleLog(SettleCriteria criteria, int sportType)
    {
        try
        {
            var insertLogCriteria = new InsertLogCriteria(criteria, sportType);

            await repository.ExecuteAsync(insertLogCriteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("Fanex.AutoRefundOtherSports.Service(Write resettle Log): ", e);
        }
    }

    public async Task WriteLog(int matchId, string message)
    {
        try
        {
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = message,
            };

            await repository.ExecuteAsync(criteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("Fanex.AutoRefundOtherSports.Service(Write Log Directly): ", e);
        }
    }
}