using System.Text;
using GesDelta.Cli;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Orchestrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = Encoding.UTF8;

const string usage =
    "Usage:\n" +
    "  setup <path-to-json>\n" +
    "  collect <topicId>\n" +
    "  detect <topicId>";

if (args.Length < 2 ||
    args[0] is not ("setup" or "collect" or "detect"))
{
    Console.WriteLine(usage);
    return 1;
}

var command = args[0];

long topicId = 0;

if (command is "collect" or "detect")
{
    if (!long.TryParse(args[1], out topicId) ||
        topicId <= 0)
    {
        Console.Error.WriteLine(
            $"Invalid topic id: '{args[1]}'");

        return 1;
    }
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var appSettings = new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
};

var builder = Host.CreateApplicationBuilder(appSettings);
builder.AddMonitoring();

using var host = builder.Build();

try
{
    await host.StartAsync(cts.Token);

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Cli started, command: {Command}", command);

    // DbContext scoped, из корневого провайдера его получить нельзя
    using var scope = host.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMonitoringService>();

    if (command == "setup")
    {
        var json = await File.ReadAllTextAsync(args[1], cts.Token);
        var request = SetupFile.Parse(json);

        var newTopicId = await service.StartMonitoringAsync(request, cts.Token);

        logger.LogInformation("Topic created, id = {TopicId}", newTopicId);
        Console.WriteLine($"Topic created, id = {newTopicId}");
        return 0;
    }

    var report = await service.CollectAsync(topicId, cts.Token);

    logger.LogInformation("Collect finished, run = {RunId}", report.RunId);
    PrintReport(report, command == "detect");
    return report.Sources.All(s => !s.FetchOk) ? 1 : 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Отменено.");
    return 1;
}
catch (Exception ex) when (ex is FormatException or ArgumentException or IOException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (DbUpdateException ex)
{
    Console.Error.WriteLine($"Database error: {ex.Message}");
    return 1;
}
finally
{
    await host.StopAsync();
}

static void PrintReport(
    CollectReport report,
    bool detailed)
{
    Console.WriteLine($"Run {report.RunId}");

    if (detailed)
    {
        Console.WriteLine("\nDetection results:");

        foreach (var item in report.Detections)
        {
            var change = item.ChangeId is long id
                ? $" (change #{id})"
                : "";

            Console.WriteLine(
                $"  [{item.Verdict,-10}] " +
                $"{item.SourceName}: {item.Title}{change}");
        }
    }

    Console.WriteLine("\nSources:");

    foreach (var s in report.Sources)
    {
        if (!s.FetchOk)
        {
            Console.WriteLine(
                $"  {s.SourceName}: FAILED ({s.FailureReason})");

            continue;
        }

        Console.WriteLine(
            $"  {s.SourceName}: " +
            $"{s.Fetched} fetched, " +
            $"{s.NewMaterials} new, " +
            $"{s.Updated} updated, " +
            $"{s.Noise} noise, " +
            $"{s.Unchanged} unchanged, " +
            $"{s.ParseFailed} parse errors, " +
            $"{s.OverBudget} over budget");
    }

    Console.WriteLine("\nTotal:");

    Console.WriteLine(
        $"  Fetched:     {report.Sources.Sum(s => s.Fetched)}\n" +
        $"  New:         {report.Sources.Sum(s => s.NewMaterials)}\n" +
        $"  Updated:     {report.Sources.Sum(s => s.Updated)}\n" +
        $"  Noise:       {report.Sources.Sum(s => s.Noise)}\n" +
        $"  Unchanged:   {report.Sources.Sum(s => s.Unchanged)}\n" +
        $"  ParseError:  {report.Sources.Sum(s => s.ParseFailed)}\n" +
        $"  OverBudget:  {report.Sources.Sum(s => s.OverBudget)}");
}