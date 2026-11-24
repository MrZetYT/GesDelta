using System.Text.Json;
using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;

namespace GesDelta.Orchestrator;

public class CollectRunner(
    GesDeltaDbContext db,
    Collector collector,
    IParser parser,
    TemporaryMaterialWriter writer,
    TimeProvider time)
{
    public async Task<CollectReport> CollectAsync(long topicId, CancellationToken ct)
    {
        if (!await db.Topics.AnyAsync(t => t.Id == topicId, ct))
            throw new ArgumentException($"Topic {topicId} not found");

        var sources = await db.Sources
            .AsNoTracking()
            .Where(s => s.TopicId == topicId && s.Enabled)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        var run = new Run
        {
            TopicId = topicId,
            Trigger = RunTrigger.Manual,
            Status = RunStatus.Running,
            StartedAt = Now()
        };
        db.Runs.Add(run);
        await db.SaveChangesAsync(ct);

        var reports = new List<SourceCollectReport>();

        try
        {
            foreach (var source in sources)
                reports.Add(await CollectSourceAsync(source, run.Id, ct));

            await CloseRunAsync(run, reports, ct);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            var now = Now();
            await db.Runs.Where(r => r.Id == run.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, RunStatus.Failed)
                    .SetProperty(r => r.Error, ex.Message)
                    .SetProperty(r => r.FinishedAt, now),
                CancellationToken.None);
            throw;
        }

        return new CollectReport(run.Id, reports);
    }

    private async Task<SourceCollectReport> CollectSourceAsync(
        Source source, long runId, CancellationToken ct)
    {
        var fetch = await collector.CollectAsync(source, ct);

        if (!fetch.Ok)
        {
            return new SourceCollectReport(
                source.Name, FetchOk: false, FailureReason: fetch.Reason.ToString(),
                Fetched: 0, ParseFailed: 0, NewMaterials: 0, NewSnapshots: 0, Unchanged: 0);
        }

        int parseFailed = 0, newMaterials = 0, newSnapshots = 0, unchanged = 0;

        foreach (var item in fetch.Items)
        {
            var parsed = parser.Parse(item, source.Type);
            if (!parsed.Ok)
            {
                parseFailed++;
                continue;
            }

            var outcome = await writer.SaveAsync(source.Id, runId, parsed.Item!, ct);
            switch (outcome)
            {
                case SaveOutcome.NewMaterial: newMaterials++; break;
                case SaveOutcome.NewSnapshot: newSnapshots++; break;
                case SaveOutcome.Unchanged: unchanged++; break;
            }
        }

        return new SourceCollectReport(
            source.Name, FetchOk: true, FailureReason: null,
            Fetched: fetch.Items.Count, ParseFailed: parseFailed,
            NewMaterials: newMaterials, NewSnapshots: newSnapshots, Unchanged: unchanged);
    }

    private async Task CloseRunAsync(
        Run run, List<SourceCollectReport> reports, CancellationToken ct)
    {
        var allFailed = reports.Count > 0 && reports.All(r => !r.FetchOk);

        run.FinishedAt = Now();
        run.TokensUsed = 0;
        run.Status = allFailed ? RunStatus.Failed : RunStatus.Succeeded;
        run.Error = allFailed ? "all sources failed" : null;
        run.Stats = JsonSerializer.Serialize(new
        {
            @checked = reports.Sum(r => r.Fetched),
            @new = reports.Sum(r => r.NewMaterials),
            updated = reports.Sum(r => r.NewSnapshots),
            failed = reports.Sum(r => r.ParseFailed)
        });

        await db.SaveChangesAsync(ct);
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}