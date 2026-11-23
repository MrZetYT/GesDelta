using GesDelta.Agents;
using GesDelta.Core.Models;
using GesDelta.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GesDelta.Tests;

public class TemporaryMaterialWriterTests : DbTestBase
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly TestClock _clock = new();

    [SetUp]
    public void ResetClock() =>
        _clock.Now = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static ParsedItem Item(
        string externalId = "ext-1",
        string clean = "Чистый текст",
        string raw = "<p>Чистый текст</p>",
        string title = "Заголовок") =>
        new(externalId, title, $"https://example.com/{externalId}", null,
            clean, raw, ContentHasher.Sha256(clean));

    private async Task<SaveOutcome> SaveAsync(long sourceId, long runId, ParsedItem item)
    {
        await using var db = NewDb();
        return await new TemporaryMaterialWriter(db, _clock)
            .SaveAsync(sourceId, runId, item, CancellationToken.None);
    }

    [Test]
    public async Task Save_NewItem_CreatesMaterialAndSnapshot()
    {
        var seed = await SeedAsync();

        var outcome = await SaveAsync(seed.SourceId, seed.RunId, Item());

        Assert.That(outcome, Is.EqualTo(SaveOutcome.NewMaterial));

        await using var check = NewDb();
        var material = await check.Materials.SingleAsync(m => m.SourceId == seed.SourceId);
        var snapshot = await check.Snapshots.SingleAsync(s => s.MaterialId == material.Id);

        Assert.That(material.ExternalId, Is.EqualTo("ext-1"));
        Assert.That(material.Title, Is.EqualTo("Заголовок"));
        Assert.That(material.Url, Is.EqualTo("https://example.com/ext-1"));
        Assert.That(snapshot.RunId, Is.EqualTo(seed.RunId));
    }

    [Test]
    public async Task Save_NewItem_StoresRawCleanTextAndHash()
    {
        var seed = await SeedAsync();
        var item = Item(clean: "Привет", raw: "<p>Привет</p>");

        await SaveAsync(seed.SourceId, seed.RunId, item);

        await using var check = NewDb();
        var snapshot = await check.Snapshots.SingleAsync(s => s.Material!.SourceId == seed.SourceId);

        Assert.That(snapshot.RawText, Is.EqualTo("<p>Привет</p>"));
        Assert.That(snapshot.CleanText, Is.EqualTo("Привет"));
        Assert.That(snapshot.ContentHash, Is.EqualTo(item.ContentHash));
    }

    [Test]
    public async Task Save_NewItem_SetsAllTimestampsToNow()
    {
        var seed = await SeedAsync();

        await SaveAsync(seed.SourceId, seed.RunId, Item());

        await using var check = NewDb();
        var material = await check.Materials.SingleAsync(m => m.SourceId == seed.SourceId);
        var snapshot = await check.Snapshots.SingleAsync(s => s.MaterialId == material.Id);
        var now = _clock.Now.UtcDateTime;

        Assert.That(material.FirstSeenAt, Is.EqualTo(now));
        Assert.That(material.LastSeenAt, Is.EqualTo(now));
        Assert.That(snapshot.FetchedAt, Is.EqualTo(now));
    }

    [Test]
    public async Task Save_SameItemAgain_IsUnchanged_AndOnlyUpdatesLastSeen()
    {
        var seed = await SeedAsync();
        await SaveAsync(seed.SourceId, seed.RunId, Item());
        var runId2 = await AddRunAsync(seed.TopicId);

        _clock.Now = _clock.Now.AddDays(7);
        var outcome = await SaveAsync(seed.SourceId, runId2, Item());

        Assert.That(outcome, Is.EqualTo(SaveOutcome.Unchanged));

        await using var check = NewDb();
        var material = await check.Materials.SingleAsync(m => m.SourceId == seed.SourceId);
        var snapshots = await check.Snapshots.Where(s => s.MaterialId == material.Id).ToListAsync();

        Assert.That(snapshots, Has.Count.EqualTo(1));
        Assert.That(material.LastSeenAt, Is.EqualTo(_clock.Now.UtcDateTime));
        Assert.That(material.FirstSeenAt, Is.EqualTo(_clock.Now.AddDays(-7).UtcDateTime));
    }

    [Test]
    public async Task Save_SameExternalIdChangedText_AddsSnapshot_NoSecondMaterial()
    {
        var seed = await SeedAsync();
        await SaveAsync(seed.SourceId, seed.RunId, Item(clean: "Версия один"));
        var runId2 = await AddRunAsync(seed.TopicId);

        var outcome = await SaveAsync(seed.SourceId, runId2, Item(clean: "Версия два"));

        Assert.That(outcome, Is.EqualTo(SaveOutcome.NewSnapshot));

        await using var check = NewDb();
        Assert.That(await check.Materials.CountAsync(m => m.SourceId == seed.SourceId), Is.EqualTo(1));

        var snapshots = await check.Snapshots
            .Where(s => s.Material!.SourceId == seed.SourceId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        Assert.That(snapshots, Has.Count.EqualTo(2));
        Assert.That(snapshots[0].CleanText, Is.EqualTo("Версия один"));
        Assert.That(snapshots[1].CleanText, Is.EqualTo("Версия два"));
        Assert.That(snapshots[1].RunId, Is.EqualTo(runId2));
    }

    [Test]
    public async Task Save_ReturnToOldText_AfterChange_AddsSnapshotAgain()
    {
        var seed = await SeedAsync();
        await SaveAsync(seed.SourceId, seed.RunId, Item(clean: "А"));
        await SaveAsync(seed.SourceId, seed.RunId, Item(clean: "Б"));

        var outcome = await SaveAsync(seed.SourceId, seed.RunId, Item(clean: "А"));

        Assert.That(outcome, Is.EqualTo(SaveOutcome.NewSnapshot));

        await using var check = NewDb();
        Assert.That(await check.Snapshots.CountAsync(s => s.Material!.SourceId == seed.SourceId),
            Is.EqualTo(3));
    }

    [Test]
    public async Task Save_SameExternalIdInOtherSource_CreatesSeparateMaterial()
    {
        var seed = await SeedAsync();
        var otherSourceId = await AddSourceAsync(seed.TopicId, "other");

        await SaveAsync(seed.SourceId, seed.RunId, Item());
        var outcome = await SaveAsync(otherSourceId, seed.RunId, Item());

        Assert.That(outcome, Is.EqualTo(SaveOutcome.NewMaterial));

        await using var check = NewDb();
        Assert.That(await check.Materials.CountAsync(m =>
            m.SourceId == seed.SourceId || m.SourceId == otherSourceId), Is.EqualTo(2));
    }

    [Test]
    public async Task Save_DifferentExternalIds_CreateDifferentMaterials()
    {
        var seed = await SeedAsync();

        await SaveAsync(seed.SourceId, seed.RunId, Item("a"));
        await SaveAsync(seed.SourceId, seed.RunId, Item("b"));

        await using var check = NewDb();
        Assert.That(await check.Materials.CountAsync(m => m.SourceId == seed.SourceId),
            Is.EqualTo(2));
    }
}