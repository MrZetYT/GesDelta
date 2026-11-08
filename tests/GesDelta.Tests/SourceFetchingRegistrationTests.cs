using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Options;
using GesDelta.Orchestrator;
using GesDelta.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class SourceFetchingRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "cli-appsettings.json"), optional: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSourceFetching(config);
        return services.BuildServiceProvider();
    }

    [TestCase(SourceType.Rss)]
    [TestCase(SourceType.Arxiv)]
    public void KeyedFetcher_ForSupportedType_IsRetryingDecorator(SourceType type)
    {
        using var provider = BuildProvider();

        var fetcher = provider.GetRequiredKeyedService<ISourceFetcher>(type);

        Assert.That(fetcher, Is.TypeOf<RetryingSourceFetcher>());
    }

    [Test]
    public void HostThrottle_IsSingleton()
    {
        using var provider = BuildProvider();

        var first = provider.GetRequiredService<HostThrottle>();
        var second = provider.GetRequiredService<HostThrottle>();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void CollectorOptions_ResolvedFromRealConfig_HasPauses()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptions<CollectorOptions>>().Value;

        Assert.That(options.PausesSec, Is.EqualTo(new[] { 2, 5, 10 }));
        Assert.That(options.HttpTimeoutSec, Is.EqualTo(30));
    }

    [Test]
    public void SourcesOptions_ResolvedFromRealConfig_HasHostDelays()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptions<SourcesOptions>>().Value;

        Assert.That(options.GetDelayMs("habr.com"), Is.EqualTo(10000));
    }
    
    [Test]
    public void FetcherLookup_ReturnsDecoratorForKnownTypesAndNullForUnknown()
    {
        using var provider = BuildProvider();
        var getFetcher = provider.GetRequiredService<Func<SourceType, ISourceFetcher?>>();

        Assert.That(getFetcher(SourceType.Rss), Is.TypeOf<RetryingSourceFetcher>());
        Assert.That(getFetcher(SourceType.Arxiv), Is.TypeOf<RetryingSourceFetcher>());
        Assert.That(getFetcher((SourceType)999), Is.Null);
    }
}