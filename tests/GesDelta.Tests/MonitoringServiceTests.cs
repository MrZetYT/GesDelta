using System.Text.Json;
using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using GesDelta.Orchestrator;
using GesDelta.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GesDelta.Tests;

public class MonitoringServiceTests : DbTestBase
{
    private static string UniqueName() => $"topic-{Guid.NewGuid():N}";

    private static SourceSetup Rss(string name = "habr") =>
        new(name, SourceType.Rss, "https://habr.com/ru/rss/hubs/machine_learning/articles/",
            """{"fullText":"auto"}""");

    private static SourceSetup Arxiv() =>
        new("arxiv", SourceType.Arxiv, "https://export.arxiv.org/api/query",
            """{"query":"neurosymbolic","maxItems":10}""");

    private static StartMonitoringRequest Request(
        string? topicName = null,
        TimeSpan? interval = null,
        params SourceSetup[] sources) =>
        new(topicName ?? UniqueName(),
            interval ?? TimeSpan.FromDays(7),
            sources.Length > 0 ? sources : [Rss(), Arxiv()]);

    private async Task<long> StartAsync(StartMonitoringRequest request)
    {
        await using var db = NewDb();
        return await new MonitoringService(db, null!)
            .StartMonitoringAsync(request, CancellationToken.None);
    }

    [Test]
    public async Task Start_CreatesTopic_WithNameIntervalAndEnabled()
    {
        var name = UniqueName();

        var topicId = await StartAsync(Request(name, TimeSpan.FromDays(7)));

        await using var check = NewDb();
        var topic = await check.Topics.SingleAsync(t => t.Id == topicId);

        Assert.That(topic.Name, Is.EqualTo(name));
        Assert.That(topic.CheckInterval, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(topic.Enabled, Is.True);
    }

    [Test]
    public async Task Start_CreatesSources_LinkedToTopic()
    {
        var topicId = await StartAsync(Request());

        await using var check = NewDb();
        var sources = await check.Sources
            .Where(s => s.TopicId == topicId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        Assert.That(sources, Has.Count.EqualTo(2));
        Assert.That(sources.Select(s => s.Name), Is.EqualTo(new[] { "habr", "arxiv" }));
        Assert.That(sources.Select(s => s.Type),
            Is.EqualTo(new[] { SourceType.Rss, SourceType.Arxiv }));
        Assert.That(sources, Has.All.Matches<Source>(s => s.Enabled));
        Assert.That(sources[1].Url, Is.EqualTo("https://export.arxiv.org/api/query"));
    }

    [Test]
    public async Task Start_StoresSourceConfig()
    {
        var topicId = await StartAsync(Request());

        await using var check = NewDb();
        var arxiv = await check.Sources.SingleAsync(
            s => s.TopicId == topicId && s.Type == SourceType.Arxiv);

        using var doc = JsonDocument.Parse(arxiv.Config!);
        Assert.That(doc.RootElement.GetProperty("query").GetString(), Is.EqualTo("neurosymbolic"));
        Assert.That(doc.RootElement.GetProperty("maxItems").GetInt32(), Is.EqualTo(10));
    }

    [Test]
    public async Task Start_WithoutConfig_StoresEmptyJsonObject()
    {
        var setup = new SourceSetup("page", SourceType.Html, "https://example.com/news", null);

        var topicId = await StartAsync(Request(sources: setup));

        await using var check = NewDb();
        var source = await check.Sources.SingleAsync(s => s.TopicId == topicId);

        using var doc = JsonDocument.Parse(source.Config!);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(doc.RootElement.EnumerateObject(), Is.Empty);
    }

    [Test]
    public async Task Start_CreatesOneDescriptionDocument_WithoutVersions()
    {
        var topicId = await StartAsync(Request());

        await using var check = NewDb();
        var documents = await check.Documents.Where(d => d.TopicId == topicId).ToListAsync();

        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Kind, Is.EqualTo(DocumentKind.Description));
        Assert.That(
            await check.DocumentVersions.CountAsync(v => v.DocumentId == documents[0].Id),
            Is.EqualTo(0));
    }

    [Test]
    public async Task Start_CreatesNoRuns()
    {
        var topicId = await StartAsync(Request());

        await using var check = NewDb();
        Assert.That(await check.Runs.CountAsync(r => r.TopicId == topicId), Is.EqualTo(0));
    }

    [Test]
    public void Start_EmptyTopicName_Throws()
    {
        Assert.CatchAsync<ArgumentException>(() => StartAsync(Request(topicName: "  ")));
    }

    [Test]
    public void Start_ZeroInterval_Throws()
    {
        Assert.CatchAsync<ArgumentException>(
            () => StartAsync(Request(interval: TimeSpan.Zero)));
    }

    [Test]
    public void Start_NegativeInterval_Throws()
    {
        Assert.CatchAsync<ArgumentException>(
            () => StartAsync(Request(interval: TimeSpan.FromHours(-1))));
    }

    [Test]
    public void Start_NoSources_Throws()
    {
        var request = new StartMonitoringRequest(UniqueName(), TimeSpan.FromDays(7), []);

        Assert.CatchAsync<ArgumentException>(() => StartAsync(request));
    }

    [Test]
    public void Start_MoreThanFiveSources_Throws()
    {
        var sources = Enumerable.Range(1, 6).Select(i => Rss($"s{i}")).ToArray();

        Assert.CatchAsync<ArgumentException>(() => StartAsync(Request(sources: sources)));
    }

    [TestCase("")]
    [TestCase("habr.com/rss")]
    [TestCase("/relative/path")]
    [TestCase("ftp://example.com/feed")]
    public void Start_InvalidUrl_Throws(string url)
    {
        var setup = new SourceSetup("bad", SourceType.Rss, url, null);

        Assert.CatchAsync<ArgumentException>(() => StartAsync(Request(sources: setup)));
    }

    [Test]
    public void Start_InvalidConfigJson_Throws()
    {
        var setup = new SourceSetup("bad", SourceType.Rss, "https://example.com/feed", "{not json");

        Assert.CatchAsync<ArgumentException>(() => StartAsync(Request(sources: setup)));
    }

    [Test]
    public async Task Start_InvalidSecondSource_WritesNothing()
    {
        var name = UniqueName();
        var bad = new SourceSetup("bad", SourceType.Rss, "https://example.com/feed", "{not json");

        Assert.CatchAsync<ArgumentException>(
            () => StartAsync(Request(name, sources: [Rss(), bad])));

        await using var check = NewDb();
        Assert.That(await check.Topics.CountAsync(t => t.Name == name), Is.EqualTo(0));
    }
}