using ONEbook.UM;

namespace AutoRefundOtherSports.Services;

public interface MaintenanceService
{
    bool IsMaintenance();
}

public class MaintenanceServiceImp : MaintenanceService
{
    public bool IsMaintenance()
    {
        var maintenanceService = new ONEbook.UM.Services.MaintenanceService(
            UMClientManager.RestClient,
            UMClientManager.CacheService,
            UMClientManager.UMClientConfigs);

        var downTime = maintenanceService.GetUnderMaintenanceTime();

        return downTime.IsUnderMaintenanceTime;
    }
}