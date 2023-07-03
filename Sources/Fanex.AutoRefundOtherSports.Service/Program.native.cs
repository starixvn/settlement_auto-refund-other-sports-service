using AutoRefundOtherSports.Models;
using Fanex.AutoRefundOtherSports.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.InitializeServices(builder.Configuration);

builder.Services.Configure<AppSettingsKey>(builder.Configuration);

builder.Services.AddHostedService<Fanex.AutoRefundOtherSports.Service.AutoRefundOtherSports>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/",
        async context => { await context.Response.WriteAsync("Fanex.AutoRefundOS.Service"); });
});

#if DEBUG
app.Run();
#else
await app.RunAsync("http://0.0.0.0:5000");
#endif