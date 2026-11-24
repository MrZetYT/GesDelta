using GesDelta.Cli;
using GesDelta.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using GesDelta.Orchestrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

if (args.Length < 2 || args[0] != "setup")
{
    Console.WriteLine("Usage: setup <path-to-json>");
    return 1;
}

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
logger.LogInformation("Cli started, command: {Command}", args[0]);

try
{
    var json = await File.ReadAllTextAsync(args[1]);
    var request = SetupFile.Parse(json);

    using var scope = host.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMonitoringService>();

    var topicId = await service.StartMonitoringAsync(request, CancellationToken.None);
    logger.LogInformation("Topic created, id = {TopicId}", topicId);
    Console.WriteLine($"Topic created, id = {topicId}");
    return 0;
}
catch (Exception ex) when (ex is FormatException or ArgumentException or IOException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    await host.StopAsync();
}
