using Microsoft.Extensions.Hosting;
using GesDelta.Orchestrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var appSettings = new HostApplicationBuilderSettings()
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
};

var builder = Host.CreateApplicationBuilder(appSettings);

builder.AddMonitoring();

using var host = builder.Build();

await host.StartAsync();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Cli started");

await host.StopAsync();
