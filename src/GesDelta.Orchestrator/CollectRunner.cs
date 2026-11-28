using System.Text.Json;
using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GesDelta.Orchestrator;

public class CollectRunner(
    GesDeltaDbContext db,
    Collector collector,
    IParser parser,
    Detector detector,
    IOptions<SourcesOptions> options,
    TimeProvider time)
{
    public async Task<CollectReport> CollectAsync(
        long topicId, CancellationToken ct)
    {
        if (!await db.Topics.AnyAsync(t => t.Id == topicId, ct))
            throw new ArgumentException($"Topic {topicId} not found");

        var sources = await db.Sources
            .AsNoTracking()
            .Where(s => s.TopicId == topicId && s.Enabled)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        var budget = new NewMaterialBudget(
            options.Value.MaxNewMaterialsPerRun);

        var run = new Run
        {
            TopicId = topicId,
            Trigger = RunTrigger.Manual,
            Status = RunStatus.Running,
            StartedAt = Now()
        };

        db.Runs.Add(run);
        await db.SaveChangesAsync(ct);

        var reports = new Dictionary<long, SourceCollectReport>();
        var groups = new List<List<DetectionCandidate>>();
        var detections = new List<MaterialDetectionReport>();

        try
        {
            // Phase 1: collect and parse all sources.
            foreach (var source in sources)
            {
                var (report, candidates) =
                    await CollectSourceAsync(source, ct);

                reports.Add(source.Id, report);
                groups.Add(candidates);
            }

            // Phase 2: detect changes in round-robin order.
            foreach (var candidate in RoundRobin.Interleave(groups))
            {
                ct.ThrowIfCancellationRequested();

                var result = await detector.DetectAsync(
                    candidate.SourceId,
                    run.Id,
                    candidate.Item,
                    budget,
                    ct);
                
                detections.Add(new MaterialDetectionReport(
                    candidate.SourceId,
                    reports[candidate.SourceId].SourceName,
                    candidate.Item.ExternalId,
                    candidate.Item.Title,
                    result.Verdict,
                    result.ChangeId));

                reports[candidate.SourceId] = ApplyVerdict(
                    reports[candidate.SourceId],
                    result.Verdict);
            }

            var orderedReports = sources
                .Select(s => reports[s.Id])
                .ToList();

            await CloseRunAsync(run, orderedReports, ct);

            return new CollectReport(run.Id, orderedReports)
            {
                Detections = detections.AsReadOnly()
            };
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();

            var now = Now();

            await db.Runs
                .Where(r => r.Id == run.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, RunStatus.Failed)
                    .SetProperty(r => r.Error, ex.Message)
                    .SetProperty(r => r.FinishedAt, now),
                    CancellationToken.None);

            throw;
        }
    }

    private async Task<(
        SourceCollectReport Report,
        List<DetectionCandidate> Candidates)>
        CollectSourceAsync(
            Source source,
            CancellationToken ct)
    {
        var fetch = await collector.CollectAsync(source, ct);

        if (!fetch.Ok)
        {
            return (
                new SourceCollectReport(
                    source.Name,
                    FetchOk: false,
                    FailureReason: fetch.Reason.ToString(),
                    Fetched: 0,
                    ParseFailed: 0,
                    NewMaterials: 0,
                    NewSnapshots: 0,
                    Unchanged: 0),
                []);
        }

        var candidates = new List<DetectionCandidate>();
        var parseFailed = 0;

        foreach (var item in fetch.Items)
        {
            ct.ThrowIfCancellationRequested();

            var parsed = parser.Parse(item, source.Type);

            if (!parsed.Ok)
            {
                parseFailed++;
                continue;
            }

            candidates.Add(new DetectionCandidate(
                source.Id,
                parsed.Item!));
        }

        var ordered = candidates
            .OrderByDescending(c => c.Item.PublishedAt)
            .ToList();

        return (
            new SourceCollectReport(
                source.Name,
                FetchOk: true,
                FailureReason: null,
                Fetched: fetch.Items.Count,
                ParseFailed: parseFailed,
                NewMaterials: 0,
                NewSnapshots: 0,
                Unchanged: 0),
            ordered);
    }

    private static SourceCollectReport ApplyVerdict(
        SourceCollectReport report,
        DetectionVerdict verdict)
    {
        return verdict switch
        {
            DetectionVerdict.New => report with
            {
                NewMaterials = report.NewMaterials + 1
            },

            DetectionVerdict.Updated => report with
            {
                Updated = report.Updated + 1,
                NewSnapshots = report.NewSnapshots + 1
            },

            DetectionVerdict.Noise => report with
            {
                Noise = report.Noise + 1,
                NewSnapshots = report.NewSnapshots + 1
            },

            DetectionVerdict.Unchanged => report with
            {
                Unchanged = report.Unchanged + 1
            },

            DetectionVerdict.ParseError => report with
            {
                ParseFailed = report.ParseFailed + 1
            },

            DetectionVerdict.OverBudget => report with
            {
                OverBudget = report.OverBudget + 1
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(verdict), verdict, null)
        };
    }

    private async Task CloseRunAsync(
        Run run,
        List<SourceCollectReport> reports,
        CancellationToken ct)
    {
        var allFailed = reports.Count > 0 &&
                        reports.All(r => !r.FetchOk);

        var newCount = reports.Sum(r => r.NewMaterials);
        var updatedCount = reports.Sum(r => r.Updated);

        var pendingCount = await db.Changes.CountAsync(
            c => c.Status == ChangeStatus.Pending &&
                 c.Material!.Source!.TopicId == run.TopicId,
            ct);

        run.FinishedAt = Now();
        run.TokensUsed = 0;

        run.Status = allFailed
            ? RunStatus.Failed
            : newCount + updatedCount == 0 && pendingCount == 0
                ? RunStatus.NoChanges
                : RunStatus.Succeeded;

        run.Error = allFailed ? "all sources failed" : null;

        run.Stats = JsonSerializer.Serialize(new
        {
            @checked = reports.Sum(r => r.Fetched),
            @new = newCount,
            updated = updatedCount,
            noise = reports.Sum(r => r.Noise),
            failed = reports.Sum(r => r.ParseFailed),
            unchanged = reports.Sum(r => r.Unchanged),
            overBudget = reports.Sum(r => r.OverBudget),
            pending = pendingCount
        });

        await db.SaveChangesAsync(ct);
    }

    private DateTime Now() =>
        time.GetUtcNow().UtcDateTime;
}