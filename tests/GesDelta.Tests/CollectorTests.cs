using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class CollectorTests
{
    private sealed class RecordingFetcher : ISourceFetcher
    {
        private readonly FetchResult _result;

        public FetchRequest? LastRequest { get; private set; }
        public int Calls { get; private set; }

        public RecordingFetcher(FetchResult? result = null)
        {
            _result = result ?? FetchResult.Success(new List<FetchedItem>());
        }

        public Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct)
        {
            Calls++;
            LastRequest = fetchRequest;
            return Task.FromResult(_result);
        }
    }

    private static Collector CreateCollector(RecordingFetcher? fetcher)
    {
        var options = Options.Create(new SourcesOptions { MaxItemsPerSource = 20 });

        return new Collector(
            type => type == SourceType.Rss ? fetcher : null,
            options,
            NullLogger<Collector>.Instance);
    }

    private static Source CreateSource(string config) => new()
    {
        Id = 7,
        Name = "test",
        Type = SourceType.Rss,
        Url = "https://example.com/feed",
        Config = config
    };

    [TestCase("{}", 20)]
    [TestCase("", 20)]
    [TestCase("   ", 20)]
    [TestCase("""{"maxItems":5}""", 5)]
    [TestCase("""{"query":"x","maxItems":10}""", 10)]
    public async Task Collect_ResolvesMaxItems(string config, int expectedMaxItems)
    {
        var fetcher = new RecordingFetcher();
        var collector = CreateCollector(fetcher);

        await collector.CollectAsync(CreateSource(config), CancellationToken.None);

        Assert.That(fetcher.LastRequest!.MaxItems, Is.EqualTo(expectedMaxItems));
    }

    [TestCase("{")]
    [TestCase("""{"maxItems":0}""")]
    [TestCase("""{"maxItems":-3}""")]
    [TestCase("""{"maxItems":"five"}""")]
    public async Task Collect_BadConfig_ReturnsInvalidConfigWithoutCallingFetcher(string config)
    {
        var fetcher = new RecordingFetcher();
        var collector = CreateCollector(fetcher);

        var result = await collector.CollectAsync(CreateSource(config), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.InvalidConfig));
        Assert.That(fetcher.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task Collect_NoFetcherForType_ReturnsInvalidConfig()
    {
        var collector = CreateCollector(fetcher: null);

        var result = await collector.CollectAsync(CreateSource("{}"), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.InvalidConfig));
    }

    [Test]
    public async Task Collect_FetcherSucceeds_ReturnsSameResult()
    {
        var expected = FetchResult.Success(new List<FetchedItem>
        {
            new(ExternalId: "a", Title: "t", Url: "https://example.com/a", PublishedAt: null, RawText: "x", FromPage: false)
        });
        var collector = CreateCollector(new RecordingFetcher(expected));

        var result = await collector.CollectAsync(CreateSource("{}"), CancellationToken.None);

        Assert.That(result, Is.SameAs(expected));
    }

    [TestCase(FetchFailureReason.Http404)]
    [TestCase(FetchFailureReason.Timeout)]
    public async Task Collect_FetcherFails_PassesReasonThrough(FetchFailureReason reason)
    {
        var collector = CreateCollector(new RecordingFetcher(FetchResult.Failure(reason)));

        var result = await collector.CollectAsync(CreateSource("{}"), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(reason));
    }

    [Test]
    public async Task Collect_PassesSourceFieldsToFetcher()
    {
        var fetcher = new RecordingFetcher();
        var collector = CreateCollector(fetcher);
        var source = CreateSource("""{"maxItems":5}""");

        await collector.CollectAsync(source, CancellationToken.None);

        var request = fetcher.LastRequest!;
        Assert.That(request.SourceId, Is.EqualTo(7));
        Assert.That(request.SourceType, Is.EqualTo(SourceType.Rss));
        Assert.That(request.Url, Is.EqualTo("https://example.com/feed"));
        Assert.That(request.Config, Is.EqualTo("""{"maxItems":5}"""));
    }

    [Test]
    public async Task Collect_EmptyConfig_FetcherGetsEmptyJsonObject()
    {
        var fetcher = new RecordingFetcher();
        var collector = CreateCollector(fetcher);

        await collector.CollectAsync(CreateSource(""), CancellationToken.None);

        Assert.That(fetcher.LastRequest!.Config, Is.EqualTo("{}"));
    }

    [Test]
    public async Task Collect_DoesNotModifySourceEntity()
    {
        var collector = CreateCollector(new RecordingFetcher());
        var source = CreateSource("");

        await collector.CollectAsync(source, CancellationToken.None);

        Assert.That(source.Config, Is.EqualTo(""));
    }
}