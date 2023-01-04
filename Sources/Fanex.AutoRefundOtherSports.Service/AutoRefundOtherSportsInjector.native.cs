using AutoMapper;
using AutoRefundOtherSports.Services;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.AspNetCore.Http;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Transport;

namespace Fanex.AutoRefundOtherSports;

public static class AutoRefundInjector
{
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
        ConfigureAutoMapper(service);

        service.AddSingleton<IDynamicRepository, DynamicRepository>();
        service.AddSingleton<MatchPoolService, MatchPoolServiceImp>();
        service.AddSingleton<LoggingService, LoggingServiceImp>();
        service.AddSingleton<VerificationService, VerificationServiceImp>();
        service.AddSingleton<AutoRefundController, AutoRefundControllerImp>();
        service.AddSingleton<CreateQueryService, CreateQueryServiceImp>();
        service.AddSingleton<RefundController, RefundControllerImp>();
        service.AddSingleton<VerificationStatusService, VerificationStatusServiceImp>();
        service.AddSingleton<MatchService, MatchServiceImp>();
        service.AddTransient<MaintenanceService, MaintenanceServiceImp>();
        service.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        service.AddSingleton<SettleService, SettleServiceImp>();
        service.AddSingleton<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.AddSingleton<GeneralService, GeneralServiceImp>();
        service.AddSingleton<RetryValidationService, RetryValidationServiceImp>();

        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        ConfigurationRedis(service, configuration);
    }

    private static void ConfigurationRedis(IServiceCollection service, IConfiguration configuration)
    {
        var redisHost = configuration.GetSection("Redis.Server").Value;
        var redisPort = configuration.GetSection("Redis.Port").Value;
        var redisPassword = configuration.GetSection("Redis.Password").Value;
        var redisDatabase = configuration.GetSection("Redis.DefaultDatabase").Value;
        service.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = $"{redisHost}:{redisPort},password={redisPassword},defaultDatabase={redisDatabase}",
            ApplicationName = configuration.GetSection("Redis.ApplicationName").Value,
        }));
        service.AddSingleton<ICacheService>(new CacheService());
    }

    private static void ConfigureAutoMapper(IServiceCollection service)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ScaleOutMappingProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        service.AddSingleton(mapper);
    }
}