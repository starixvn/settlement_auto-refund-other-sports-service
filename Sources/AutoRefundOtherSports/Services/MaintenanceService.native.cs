using Fanex.Caching;
using Microsoft.Extensions.Options;
using ONEbook.UM;
using ONEbook.UM.Services;

namespace AutoRefundOtherSports.Services;

public interface IMaintenanceService
{
    Task<bool> IsMaintenance();
}

public class MaintenanceServiceImp : IMaintenanceService
{
    private readonly ICache cache;

    private readonly AppSettingsKey appSettingsKey;

    public MaintenanceServiceImp(ICache cache,
        IOptions<AppSettingsKey> appSettingsKey)
    {
        this.cache = cache;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<bool> IsMaintenance()
    {
        var maintenanceService = new MaintenanceService(
            UMClientManager.RestClient,
            UMClientManager.CacheService,
            UMClientManager.UMClientConfigs);

        var downTime = await maintenanceService.GetUnderMaintenanceTimeAsync();

        var isDeploySpName = await IsDeploySpName();
        return downTime.IsUnderMaintenanceTime || isDeploySpName;
    }

    private async Task<bool> IsDeploySpName()
    {
        var isExistedKey = await cache.ContainAsync(appSettingsKey.DeploymentSpName);
        if (isExistedKey)
        {
            var (_, value) = await cache.TryGetAsync<int>(appSettingsKey.DeploymentSpName);
            return value > 0;
        }

        return false;
    }
}