using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Options;
using GesDelta.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GesDelta.Orchestrator;

public static class SourceFetchingExtensions
{
    public static void AddSourceFetching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SourcesOptions>(configuration.GetSection("Sources"));
        services.Configure<CollectorOptions>(configuration.GetSection("Orchestration:Collector"));

        services.AddSingleton<HostThrottle>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<Func<SourceType, ISourceFetcher?>>(sp => type => sp.GetKeyedService<ISourceFetcher>(type));
        
        AddFetcher<RssSourceFetcher>(services, SourceType.Rss);
        AddFetcher<ArxivSourceFetcher>(services, SourceType.Arxiv);
    }
    
    private static void AddFetcher<TFetcher>(IServiceCollection services, SourceType type)
        where TFetcher : class, ISourceFetcher
    {
        services.AddHttpClient<TFetcher>((sp, client) =>
        {
            var sources = sp.GetRequiredService<IOptions<SourcesOptions>>().Value;
            var collector = sp.GetRequiredService<IOptions<CollectorOptions>>().Value;

            client.Timeout = TimeSpan.FromSeconds(collector.HttpTimeoutSec);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", sources.UserAgent);
        });
        services.AddKeyedTransient<ISourceFetcher>(type, (sp, _) => new RetryingSourceFetcher(
            sp.GetRequiredService<TFetcher>(),
            sp.GetRequiredService<HostThrottle>(),
            sp.GetRequiredService<IOptions<CollectorOptions>>(),
            sp.GetRequiredService<TimeProvider>()));
    }
}