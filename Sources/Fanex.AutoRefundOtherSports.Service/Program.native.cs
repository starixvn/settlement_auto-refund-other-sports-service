using AutoRefundOtherSports.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fanex.AutoRefundOtherSports.Service
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseUrls($"http://0.0.0.0:5000");
                        webBuilder.UseStartup<Startup>();
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services
                            .Configure<AppSettingsKey>(hostContext.Configuration)
                            .AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettingsKey>>().Value);
                        services.AddHostedService<AutoRefundOtherSports>();
                        services.InitializeServices(hostContext.Configuration);
                    })
                    .UseSystemd();
    }
}