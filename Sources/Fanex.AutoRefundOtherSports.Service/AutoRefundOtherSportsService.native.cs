using AutoRefundOtherSports.Services;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;
using ONEbook.UM.Services;

namespace Fanex.AutoRefundOtherSports.Service;

public class AutoRefundOtherSports(
    AutoRefundController autoRefundController,
    IConfiguration configuration,
    IMaintenanceService maintenanceService,
    MatchService matchService)
    : BackgroundService
{
    private int counter;

    private int timeCounter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(configuration.GetValue<int>("ServiceInterval")));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await HandleAutoAsync();
        }
    }

    private async Task HandleAutoAsync()
    {
        try
        {
            var isMaintenance = await maintenanceService.IsUnderMaintenanceAsync();
            if (!isMaintenance)
            {
                await autoRefundController.ProcessAsync();
                HandleMissedMatch();
                HandleGarbageCollector();
            }
        }
        catch (Exception exception)
        {
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .ToString();
            await LogService.ErrorAsync(logMessage);
        }
    }

    private void HandleGarbageCollector()
    {
        if (counter >= int.Parse(configuration.GetSection("GarbageCollectorInterval").Value!))
        {
            counter = 0;
            GC.WaitForPendingFinalizers();
        }

        counter += int.Parse(configuration.GetSection("ServiceInterval").Value!);
    }

    private void HandleMissedMatch()
    {
        if (timeCounter >= int.Parse(configuration.GetSection("CheckMissedMatchInterval").Value!))
        {
            timeCounter = 0;
            matchService.CheckMissedMatch();
        }

        timeCounter += int.Parse(configuration.GetSection("ServiceInterval").Value!);
    }
}