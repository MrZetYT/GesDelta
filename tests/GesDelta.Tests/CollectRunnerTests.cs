using System.Net;
using System.Text;
using System.Text.Json;
using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Orchestrator;
using GesDelta.Tests.Support;
using GesDelta.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class CollectRunnerTests : DbTestBase
{
    private const string HabrConfig = """{"fullText":"never","maxItems":20}""";
    private const string ArxivConfig = """{"query":"neurosymbolic","maxItems":10}""";

    private sealed record SourceDef(
        string Name, SourceType Type, string Url, string Config, bool Enabled = true);

    private static SourceDef Habr(string name = "habr", bool enabled = true) =>
        new(name, SourceType.Rss, "https://habr.test/feed", HabrConfig, enabled);

    private static SourceDef Arxiv() =>
        new("arxiv", SourceType.Arxiv, "https://arxiv.test/api/query", ArxivConfig);

    private static SourceDef Broken() =>
        new("broken", SourceType.Rss, "https://broken.test/feed", HabrConfig);

    private static HttpResponseMessage FileResponse(string fileName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", fileName);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText(path), Encoding.UTF8, "application/xml")
        };
    }

    private static HttpResponseMessage Route(Uri uri) => uri.Host switch
    {
        "habr.test" => FileResponse("habr_feed.xml"),
        "arxiv.test" => FileResponse("arxiv_neurosymbolic.xml"),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    };

    private async Task<long> SeedTopicAsync(params SourceDef[] sources)
    {
        await using var db = NewDb();
        var topic = new Topic { Name = $"topic-{Guid.NewGuid():N}" };
        db.Topics.Add(topic);
        db.Sources.AddRange(sources.Select(s => new Source
        {
            Topic = topic, Name = s.Name, Type = s.Type, Url = s.Url,
            Config = s.Config, Enabled = s.Enabled
        }));
        await db.SaveChangesAsync();
        return topic.Id;
    }

    private async Task<CollectReport> CollectAsync(long topicId)
    {
        var http = new HttpClient(new RoutingHandler(Route));

        ISourceFetcher? GetFetcher(SourceType type) => type switch
        {
            SourceType.Rss => new RssSourceFetcher(http),
            SourceType.Arxiv => new ArxivSourceFetcher(http),
            _ => null
        };

        await using var db = NewDb();
        var collector = new Collector(
            GetFetcher, Options.Create(new SourcesOptions()), NullLogger<Collector>.Instance);
        var runner = new CollectRunner(
            db, collector, new Parser(),
            new TemporaryMaterialWriter(db, TimeProvider.System), TimeProvider.System);

        return await runner.CollectAsync(topicId, CancellationToken.None);
    }

    private async Task<int> CountMaterialsAsync(long topicId)
    {
        await using var db = NewDb();
        return await db.Materials.CountAsync(m => m.Source!.TopicId == topicId);
    }

    private async Task<int> CountSnapshotsAsync(long topicId)
    {
        await using var db = NewDb();
        return await db.Snapshots.CountAsync(s => s.Material!.Source!.TopicId == topicId);
    }

    [Test]
    public async Task Collect_FirstRun_SavesMaterialsFromAllSources()
    {
        var topicId = await SeedTopicAsync(Habr(), Arxiv());

        var report = await CollectAsync(topicId);

        Assert.That(report.Sources, Has.Count.EqualTo(2));
        Assert.That(report.Sources, Has.All.Matches<SourceCollectReport>(s => s.FetchOk));
        Assert.That(report.Sources, Has.All.Matches<SourceCollectReport>(s => s.Fetched > 0));
        Assert.That(report.Sources, Has.All.Matches<SourceCollectReport>(
            s => s.NewMaterials + s.ParseFailed == s.Fetched));

        var saved = report.Sources.Sum(s => s.NewMaterials);
        Assert.That(saved, Is.GreaterThan(0));
        Assert.That(await CountMaterialsAsync(topicId), Is.EqualTo(saved));
        Assert.That(await CountSnapshotsAsync(topicId), Is.EqualTo(saved));
    }

    [Test]
    public async Task Collect_ReportKeepsSourceNames_InOrder()
    {
        var topicId = await SeedTopicAsync(Habr("first"), Arxiv());

        var report = await CollectAsync(topicId);

        Assert.That(report.Sources.Select(s => s.SourceName),
            Is.EqualTo(new[] { "first", "arxiv" }));
    }

    [Test]
    public async Task Collect_SecondRunOnSameData_IsAllUnchanged()
    {
        var topicId = await SeedTopicAsync(Habr(), Arxiv());
        var first = await CollectAsync(topicId);
        var materials = await CountMaterialsAsync(topicId);
        var snapshots = await CountSnapshotsAsync(topicId);

        var second = await CollectAsync(topicId);

        Assert.That(second.RunId, Is.Not.EqualTo(first.RunId));
        Assert.That(second.Sources.Sum(s => s.NewMaterials), Is.EqualTo(0));
        Assert.That(second.Sources.Sum(s => s.NewSnapshots), Is.EqualTo(0));
        Assert.That(second.Sources.Sum(s => s.Unchanged), Is.EqualTo(materials));
        Assert.That(await CountMaterialsAsync(topicId), Is.EqualTo(materials));
        Assert.That(await CountSnapshotsAsync(topicId), Is.EqualTo(snapshots));
    }

    [Test]
    public async Task Collect_CreatesRun_AndClosesItAsSucceeded()
    {
        var topicId = await SeedTopicAsync(Habr());

        var report = await CollectAsync(topicId);

        await using var check = NewDb();
        var run = await check.Runs.SingleAsync(r => r.Id == report.RunId);

        Assert.That(run.TopicId, Is.EqualTo(topicId));
        Assert.That(run.Trigger, Is.EqualTo(RunTrigger.Manual));
        Assert.That(run.Status, Is.EqualTo(RunStatus.Succeeded));
        Assert.That(run.FinishedAt, Is.Not.Null);
        Assert.That(run.Error, Is.Null);
    }

    [Test]
    public async Task Collect_Snapshots_BelongToThisRun()
    {
        var topicId = await SeedTopicAsync(Habr());

        var report = await CollectAsync(topicId);

        await using var check = NewDb();
        var runIds = await check.Snapshots
            .Where(s => s.Material!.Source!.TopicId == topicId)
            .Select(s => s.RunId)
            .Distinct()
            .ToListAsync();

        Assert.That(runIds, Is.EqualTo(new[] { report.RunId }));
    }

    [Test]
    public async Task Collect_WritesStatsJson_WithNewCount()
    {
        var topicId = await SeedTopicAsync(Habr());

        var report = await CollectAsync(topicId);

        await using var check = NewDb();
        var run = await check.Runs.SingleAsync(r => r.Id == report.RunId);

        using var stats = JsonDocument.Parse(run.Stats);
        Assert.That(stats.RootElement.GetProperty("new").GetInt32(),
            Is.EqualTo(report.Sources.Sum(s => s.NewMaterials)));
    }

    [Test]
    public async Task Collect_OneSourceFails_OthersAreStillSaved_RunSucceeds()
    {
        var topicId = await SeedTopicAsync(Broken(), Habr());

        var report = await CollectAsync(topicId);

        Assert.That(report.Sources[0].FetchOk, Is.False);
        Assert.That(report.Sources[0].FailureReason, Is.Not.Null.And.Not.Empty);
        Assert.That(report.Sources[1].FetchOk, Is.True);
        Assert.That(report.Sources[1].NewMaterials, Is.GreaterThan(0));

        await using var check = NewDb();
        var run = await check.Runs.SingleAsync(r => r.Id == report.RunId);
        Assert.That(run.Status, Is.EqualTo(RunStatus.Succeeded));
    }

    [Test]
    public async Task Collect_AllSourcesFail_RunIsFailed_AndNothingSaved()
    {
        var topicId = await SeedTopicAsync(Broken());

        var report = await CollectAsync(topicId);

        Assert.That(report.Sources, Has.All.Matches<SourceCollectReport>(s => !s.FetchOk));
        Assert.That(await CountMaterialsAsync(topicId), Is.EqualTo(0));

        await using var check = NewDb();
        var run = await check.Runs.SingleAsync(r => r.Id == report.RunId);
        Assert.That(run.Status, Is.EqualTo(RunStatus.Failed));
        Assert.That(run.Error, Is.Not.Null.And.Not.Empty);
        Assert.That(run.FinishedAt, Is.Not.Null);
    }

    [Test]
    public async Task Collect_AfterFailedRun_NextRunIsAllowed()
    {
        var topicId = await SeedTopicAsync(Broken());
        await CollectAsync(topicId);

        Assert.DoesNotThrowAsync(() => CollectAsync(topicId));
    }

    [Test]
    public async Task Collect_DisabledSource_IsSkipped()
    {
        var topicId = await SeedTopicAsync(Habr("on"), Habr("off", enabled: false));

        var report = await CollectAsync(topicId);

        Assert.That(report.Sources.Select(s => s.SourceName), Is.EqualTo(new[] { "on" }));
    }

    [Test]
    public void Collect_UnknownTopic_Throws()
    {
        Assert.CatchAsync<ArgumentException>(() => CollectAsync(topicId: -1));
    }
}