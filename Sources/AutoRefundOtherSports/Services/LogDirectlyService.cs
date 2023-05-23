using Fanex.Logging;

namespace AutoRefundOtherSports.Services;
public interface LogDirectlyService
{
    void WriteLog(int matchId, string message);
}

public class LogDirectlyServiceImp : LogDirectlyService
{
    private readonly IDynamicRepository repository;

    public LogDirectlyServiceImp(IDynamicRepository repository)
    {
        this.repository = repository;
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
            LogService.Error("Fanex.AutoEarlyOtherSports.Service(Write Log Directly): ", e);
        }
    }
}