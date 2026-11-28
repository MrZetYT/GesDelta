using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Tests.Support;
using GesDelta.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class DetectorTests : DbTestBase
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly TestClock _clock = new();

    private static readonly string Baseline =
        new string('a', 500);

    [SetUp]
    public void ResetClock()
    {
        _clock.Now = new DateTimeOffset(
            2026, 11, 28, 10, 0, 0, TimeSpan.Zero);
    }

    private static ParsedItem Item(
        string clean,
        string externalId = "ext-1")
    {
        return new ParsedItem(
            externalId,
            "Title",
            $"https://example.com/{externalId}",
            null,
            clean,
            $"<p>{clean}</p>",
            ContentHasher.Sha256(clean));
    }

    private async Task<DetectionResult> DetectAsync(
        long sourceId,
        long runId,
        ParsedItem item,
        NewMaterialBudget budget)
    {
        await using var db = NewDb();

        var options = Options.Create(
            new DetectorOptions());

        var judge = new SignificanceJudge(
            new DiffPlexTextDiffer(),
            new TextSimilarity(),
            options);

        var detector = new Detector(
            db, judge, options, _clock);

        return await detector.DetectAsync(
            sourceId,
            runId,
            item,
            budget,
            CancellationToken.None);
    }

    [Test]
    public async Task NewItem_CreatesMaterialSnapshotAndChange()
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(10);
        var item = Item(Baseline);

        var result = await DetectAsync(
            seed.SourceId, seed.RunId, item, budget);

        await using var db = NewDb();

        var material = await db.Materials.SingleAsync(m => m.SourceId == seed.SourceId);

        var snapshot = await db.Snapshots.SingleAsync(s => s.MaterialId == material.Id);

        var change = await db.Changes.SingleAsync(c => c.MaterialId == material.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Verdict,
                Is.EqualTo(DetectionVerdict.New));

            Assert.That(result.ChangeId,
                Is.EqualTo(change.Id));

            Assert.That(change.Kind,
                Is.EqualTo(ChangeKind.New));

            Assert.That(change.Status,
                Is.EqualTo(ChangeStatus.Pending));

            Assert.That(change.PrevSnapshotId, Is.Null);

            Assert.That(change.NewSnapshotId,
                Is.EqualTo(snapshot.Id));

            Assert.That(change.RunId,
                Is.EqualTo(seed.RunId));

            Assert.That(snapshot.CleanText,
                Is.EqualTo(item.CleanText));

            Assert.That(snapshot.RawText,
                Is.EqualTo(item.RawText));

            Assert.That(snapshot.ContentHash,
                Is.EqualTo(item.ContentHash));

            Assert.That(material.FirstSeenAt,
                Is.EqualTo(_clock.Now.UtcDateTime));

            Assert.That(material.LastSeenAt,
                Is.EqualTo(_clock.Now.UtcDateTime));

            Assert.That(budget.Remaining, Is.EqualTo(9));
        });
    }

    [Test]
    public async Task SameText_OnlyUpdatesLastSeen()
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(10);
        var item = Item(Baseline);

        await DetectAsync(
            seed.SourceId, seed.RunId, item, budget);

        var runId = await AddRunAsync(seed.TopicId);

        _clock.Now = _clock.Now.AddDays(7);

        var result = await DetectAsync(
            seed.SourceId, runId, item, budget);

        await using var db = NewDb();

        var material = await db.Materials.SingleAsync(m => m.SourceId == seed.SourceId);

        Assert.That(result.Verdict,
            Is.EqualTo(DetectionVerdict.Unchanged));

        Assert.That(result.ChangeId, Is.Null);

        Assert.That(material.LastSeenAt,
            Is.EqualTo(_clock.Now.UtcDateTime));

        Assert.That(material.FirstSeenAt,
            Is.EqualTo(_clock.Now.AddDays(-7).UtcDateTime));

        Assert.That(await db.Snapshots.CountAsync(s => s.MaterialId == material.Id), Is.EqualTo(1));

        Assert.That(await db.Changes.CountAsync(c => c.MaterialId == material.Id), Is.EqualTo(1));

        Assert.That(budget.Remaining, Is.EqualTo(9));
    }

    [Test]
    public async Task MinorAddition_CreatesOnlySnapshot()
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(10);

        await DetectAsync(
            seed.SourceId, seed.RunId,
            Item(Baseline), budget);

        var runId = await AddRunAsync(seed.TopicId);

        _clock.Now = _clock.Now.AddDays(7);

        var current = Baseline + "\n" + new string('b', 100);

        var result = await DetectAsync(
            seed.SourceId, runId, Item(current), budget);

        await using var db = NewDb();

        var material = await db.Materials.SingleAsync(m => m.SourceId == seed.SourceId);

        Assert.That(result.Verdict,
            Is.EqualTo(DetectionVerdict.Noise));

        Assert.That(result.ChangeId, Is.Null);

        Assert.That(await db.Snapshots.CountAsync(s => s.MaterialId == material.Id), Is.EqualTo(2));

        Assert.That(await db.Changes.CountAsync(c => c.MaterialId == material.Id), Is.EqualTo(1));

        Assert.That(material.LastSeenAt,
            Is.EqualTo(_clock.Now.UtcDateTime));

        Assert.That(budget.Remaining, Is.EqualTo(9));
    }

    [Test]
    public async Task AccumulatedNoise_CreatesUpdatedChange()
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(1);

        var initial = await DetectAsync(
            seed.SourceId, seed.RunId,
            Item(Baseline), budget);

        var first = new string('b', 150);
        var second = new string('c', 150);

        var runId2 = await AddRunAsync(seed.TopicId);

        _clock.Now = _clock.Now.AddDays(7);

        var noise = await DetectAsync(
            seed.SourceId, runId2,
            Item($"{Baseline}\n{first}"), budget);

        var runId3 = await AddRunAsync(seed.TopicId);

        _clock.Now = _clock.Now.AddDays(7);

        var updated = await DetectAsync(
            seed.SourceId, runId3,
            Item($"{Baseline}\n{first}\n{second}"), budget);

        await using var db = NewDb();

        var material = await db.Materials.SingleAsync(m => m.SourceId == seed.SourceId);

        var snapshots = await db.Snapshots
            .Where(s => s.MaterialId == material.Id)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var changes = await db.Changes
            .Where(c => c.MaterialId == material.Id)
            .OrderBy(c => c.Id)
            .ToListAsync();

        Assert.That(initial.Verdict,
            Is.EqualTo(DetectionVerdict.New));

        Assert.That(noise.Verdict,
            Is.EqualTo(DetectionVerdict.Noise));

        Assert.That(updated.Verdict,
            Is.EqualTo(DetectionVerdict.Updated));

        Assert.That(updated.AddedFragments,
            Is.EqualTo(new[] { first, second }));

        Assert.That(snapshots, Has.Count.EqualTo(3));
        Assert.That(changes, Has.Count.EqualTo(2));

        Assert.That(changes[1].Kind,
            Is.EqualTo(ChangeKind.Updated));

        Assert.That(changes[1].Status,
            Is.EqualTo(ChangeStatus.Pending));

        Assert.That(changes[1].PrevSnapshotId,
            Is.EqualTo(snapshots[0].Id));

        Assert.That(changes[1].NewSnapshotId,
            Is.EqualTo(snapshots[2].Id));

        Assert.That(updated.ChangeId,
            Is.EqualTo(changes[1].Id));

        Assert.That(budget.Remaining, Is.Zero);
    }

    [TestCase(0)]
    [TestCase(99)]
    public async Task InvalidText_DoesNotModifyDatabase(
        int length)
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(10);

        await DetectAsync(
            seed.SourceId, seed.RunId,
            Item(Baseline), budget);

        var previousSeen = _clock.Now.UtcDateTime;

        _clock.Now = _clock.Now.AddDays(7);

        var runId = await AddRunAsync(seed.TopicId);

        var result = await DetectAsync(
            seed.SourceId, runId,
            Item(new string('a', length)), budget);

        await using var db = NewDb();

        var material = await db.Materials.SingleAsync(m => m.SourceId == seed.SourceId);

        Assert.That(result.Verdict,
            Is.EqualTo(DetectionVerdict.ParseError));

        Assert.That(result.ChangeId, Is.Null);

        Assert.That(material.LastSeenAt,
            Is.EqualTo(previousSeen));

        Assert.That(await db.Snapshots.CountAsync(s => s.MaterialId == material.Id), Is.EqualTo(1));

        Assert.That(await db.Changes.CountAsync(c => c.MaterialId == material.Id), Is.EqualTo(1));

        Assert.That(budget.Remaining, Is.EqualTo(9));
    }
    
    [Test]
    public async Task ExhaustedBudget_DoesNotCreateMaterial()
    {
        var seed = await SeedAsync();
        var budget = new NewMaterialBudget(1);

        await DetectAsync(
            seed.SourceId, seed.RunId,
            Item(Baseline, "first"), budget);

        var result = await DetectAsync(
            seed.SourceId, seed.RunId,
            Item(Baseline, "second"), budget);

        await using var db = NewDb();

        Assert.That(result.Verdict,
            Is.EqualTo(DetectionVerdict.OverBudget));

        Assert.That(result.ChangeId, Is.Null);

        Assert.That(await db.Materials.CountAsync(
            m => m.SourceId == seed.SourceId), Is.EqualTo(1));

        Assert.That(await db.Materials.AnyAsync(
            m => m.SourceId == seed.SourceId &&
                 m.ExternalId == "second"), Is.False);

        Assert.That(await db.Snapshots.CountAsync(
                s => s.Material!.SourceId == seed.SourceId),
            Is.EqualTo(1));

        Assert.That(await db.Changes.CountAsync(
                c => c.Material!.SourceId == seed.SourceId),
            Is.EqualTo(1));

        Assert.That(budget.Remaining, Is.Zero);
    }
}