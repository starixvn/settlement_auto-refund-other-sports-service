using AutoRefundOtherSports.Services;
using Fanex.Caching;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using ONEbook.UM;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Transport;
using SharpRaven;

namespace Fanex.AutoRefundOtherSports.Service;

public class AutoRefundOtherSports : IHostedService
{
    private readonly MaintenanceService maintenanceService;

    private readonly AutoRefundController autoRefundController;

    private readonly IConfiguration configuration;

    private readonly MatchService matchService;

    private bool running;

    private int counter;

    private int timeCounter;

    public AutoRefundOtherSports(
        AutoRefundController autoRefundController,
        IConfiguration configuration,
        MaintenanceService maintenanceService,
        MatchService matchService)
    {
        this.autoRefundController = autoRefundController;
        this.maintenanceService = maintenanceService;
        this.configuration = configuration;
        this.matchService = matchService;
        InitializeExtraComponent();
        InitializeSetting();
    }

    private void InitializeSetting()
    {
        StartUmSetting();
        StartLogSetting();
    }

    private static void InitializeExtraComponent()
    {
        TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
    }

    private static void TaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        e.Exception.Handle(x =>
        {
            LogService.Error(x.Message, x);
            return true;
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        running = true;
        var autoRefundThread = new Thread(RefundThreadProcess);
        Console.WriteLine($"AutoRefund Other Sports Service Starting at {DateTime.Now}");
        autoRefundThread.Start();
        return Task.CompletedTask;
    }

    private void RefundThreadProcess()
    {
        while (running)
        {
            if (!maintenanceService.IsMaintenance())
            {
                MainProcess();
                Thread.Sleep(int.Parse(configuration.GetSection("ServiceInterval").Value!));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        running = false;
        Console.WriteLine($"AutoRefund Other Sports Service Stopping at {DateTime.Now}");
        return Task.CompletedTask;
    }

    private void MainProcess()
    {
        try
        {
            autoRefundController.ProcessAsync();
            HandleMissedMatch();
            HandleGarbageCollector();
        }
        catch (Exception exception)
        {
            LogService.ErrorAsync("Exception in main thread - Process:", exception);
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

    private void StartUmSetting()
    {
        var umConfig = new UMClientConfiguration().Initialize(configuration);

        UMClientManager
           .UseConfig(umConfig)
           .UseCacheService(new UMCacheService(new CacheService()))
           .UseRestClient(new UMRestClient(umConfig));
    }

    private void StartLogSetting()
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn(configuration.GetSection("SentryToken").Value)
        };

        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory(configuration.GetSection("SiteCategory").Value).Use(logEngine);
    }
}