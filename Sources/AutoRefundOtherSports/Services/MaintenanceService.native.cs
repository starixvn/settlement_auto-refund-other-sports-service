using ONEbook.UM;
using ONEbook.UM.Services;

namespace AutoRefundOtherSports.Services;

public interface IMaintenanceService
{
    Task<bool> IsMaintenance();
}

public class MaintenanceServiceImp : IMaintenanceService
{
    public async Task<bool> IsMaintenance()
    {
        var maintenanceService = new MaintenanceService(
            UMClientManager.RestClient,
            UMClientManager.CacheService,
            UMClientManager.UMClientConfigs);

        var downTime = await maintenanceService.GetUnderMaintenanceTimeAsync();

        return downTime.IsUnderMaintenanceTime;
    }
}