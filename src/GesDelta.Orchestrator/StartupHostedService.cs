using Microsoft.Extensions.Hosting;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GesDelta.Orchestrator;

public class StartupHostedService : IHostedService
{
    private readonly ILogger<StartupHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    public StartupHostedService(ILogger<StartupHostedService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GesDeltaDbContext>();

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        
        await context.Database.MigrateAsync(cancellationToken);
        
        _logger.LogInformation($"{pending.Count()} migrations executed.");
    }
    public Task StopAsync(CancellationToken cancellationToken)=> Task.CompletedTask;
}