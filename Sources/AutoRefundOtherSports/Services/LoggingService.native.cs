using AutoRefundOtherSports.Exceptions;
using Fanex.Logging;

namespace AutoRefundOtherSports.Services;

public interface LoggingService
{
    Task WriteLog(Match match, string message);

    Task WriteLog(Match match, AutoRefundException exception);

    Task Update(Match match, string remark, int status);

    void WriteLog(int matchId, string message);

    void InsertResettleLog(SettleCriteria criteria, int sportType);
}

public class LoggingServiceImp : LoggingService
{
    private readonly IDynamicRepository repository;

    private readonly CreateQueryService createQueryService;

    public LoggingServiceImp(IDynamicRepository repository, CreateQueryService createQueryService)
    {
        this.repository = repository;
        this.createQueryService = createQueryService;
    }

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

    public void InsertResettleLog(SettleCriteria criteria, int sportType)
    {
        try
        {
            var insertLogCriteria = new InsertLogCriteria(criteria, sportType);

            repository.Execute(insertLogCriteria);
        }
        catch (Exception e)
        {
            LogService.Error("Fanex.AutoRefundOtherSports.Service(Write resettle Log): ", e);
        }
    }

    public void WriteLog(int matchId, string message)
    {
        try
        {
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = message,
            };

            repository.Execute(criteria);
        }
        catch (Exception e)
        {
            LogService.Error("Fanex.AutoRefundOtherSports.Service(Write Log Directly): ", e);
        }
    }
}