using AutoRefundOtherSports.Models;
using Fanex.Caching;
using Microsoft.Extensions.Options;
using ONEbook.UM;
using ONEbook.UM.Services;

namespace AutoRefundOtherSports.Services
{
    public interface IMaintenanceService
    {
        bool IsMaintenance { get; }
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

        public bool IsMaintenance
        {
            get
            {
                var maintenanceService = new MaintenanceService(
                    UMClientManager.RestClient,
                    UMClientManager.CacheService,
                    UMClientManager.UMClientConfigs);

                var downTime = maintenanceService.GetUnderMaintenanceTime();

                return downTime.IsUnderMaintenanceTime;
            }
        }
    }
}