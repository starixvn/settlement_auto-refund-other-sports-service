using ONEbook.UM;
using ONEbook.UM.Services;

namespace AutoRefundOtherSports.Services;

public interface IMaintenanceService
{
    bool IsMaintenance();
}

public class MaintenanceServiceImp : IMaintenanceService
{
    public bool IsMaintenance()
    {
        var maintenanceService = new MaintenanceService(
            UMClientManager.RestClient,
            UMClientManager.CacheService,
            UMClientManager.UMClientConfigs);

        var downTime = maintenanceService.GetUnderMaintenanceTime();

        return downTime.IsUnderMaintenanceTime;
    }
}