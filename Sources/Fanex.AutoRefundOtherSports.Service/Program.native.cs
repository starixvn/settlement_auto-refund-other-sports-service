using AutoRefundOtherSports.Models;
using Fanex.AutoRefundOtherSports.Service;
using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using ONEbook.UM.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddOneBookUmForService(builder.Configuration);

services.AddFanexLogSentry(builder.Configuration);

services.AddFanexSql(builder.Configuration);

services.AddConfigRedisCache(builder.Configuration);

services.AddAutoMapper();

services.InitializeServices(builder.Configuration);

services.Configure<AppSettingsKey>(builder.Configuration);

services.AddHostedService<Fanex.AutoRefundOtherSports.Service.AutoRefundOtherSports>();
services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");

await app.RunAsync();