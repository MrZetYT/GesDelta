using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GesDelta.Agents;

public sealed class Detector(
    GesDeltaDbContext db,
    ISignificanceJudge significanceJudge,
    IOptions<DetectorOptions> options,
    TimeProvider time)
{
    private readonly DetectorOptions _options = options.Value;

    public async Task<DetectionResult> DetectAsync(
        long sourceId,
        long runId,
        ParsedItem item,
        NewMaterialBudget budget,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.CleanText))
        {
            return new DetectionResult(
                DetectionVerdict.ParseError);
        }

        var material = await db.Materials
            .FirstOrDefaultAsync(
                m => m.SourceId == sourceId &&
                     m.ExternalId == item.ExternalId,
                ct);

        var now = time.GetUtcNow().UtcDateTime;

        if (material is null)
        {
            return await SaveNewAsync(
                sourceId, runId, item, budget, now, ct);
        }

        return await DetectExistingAsync(
            material, runId, item, now, ct);
    }

    private async Task<DetectionResult> SaveNewAsync(
        long sourceId,
        long runId,
        ParsedItem item,
        NewMaterialBudget budget,
        DateTime now,
        CancellationToken ct)
    {
        if (budget.IsExhausted)
        {
            return new DetectionResult(
                DetectionVerdict.OverBudget);
        }

        var material = new Material
        {
            SourceId = sourceId,
            ExternalId = item.ExternalId,
            Title = item.Title,
            Url = item.Url,
            PublishedAt = item.PublishedAt,
            FirstSeenAt = now,
            LastSeenAt = now
        };

        var snapshot = CreateSnapshot(
            material, runId, item, now);

        var change = new Change
        {
            Material = material,
            RunId = runId,
            Kind = ChangeKind.New,
            NewSnapshot = snapshot,
            Status = ChangeStatus.Pending,
            CreatedAt = now
        };

        db.Materials.Add(material);
        db.Snapshots.Add(snapshot);
        db.Changes.Add(change);

        await db.SaveChangesAsync(ct);

        budget.Consume();

        return new DetectionResult(
            DetectionVerdict.New,
            change.Id);
    }

    private async Task<DetectionResult> DetectExistingAsync(
        Material material,
        long runId,
        ParsedItem item,
        DateTime now,
        CancellationToken ct)
    {
        var lastSnapshot = await db.Snapshots
            .Where(s => s.MaterialId == material.Id)
            .OrderByDescending(s => s.FetchedAt)
            .ThenByDescending(s => s.Id)
            .FirstAsync(ct);

        if (item.CleanText.Length <
            lastSnapshot.CleanText.Length *
            _options.MinTextLengthRatio)
        {
            return new DetectionResult(
                DetectionVerdict.ParseError);
        }

        if (lastSnapshot.ContentHash == item.ContentHash)
        {
            material.LastSeenAt = now;

            await db.SaveChangesAsync(ct);

            return new DetectionResult(
                DetectionVerdict.Unchanged);
        }

        var baselineSnapshot = await db.Changes
            .Where(c => c.MaterialId == material.Id)
            .OrderByDescending(c => c.Id)
            .Select(c => c.NewSnapshot!)
            .FirstAsync(ct);

        var significance = significanceJudge.Evaluate(
            baselineSnapshot.CleanText,
            item.CleanText);

        var snapshot = CreateSnapshot(
            material, runId, item, now);

        material.LastSeenAt = now;

        db.Snapshots.Add(snapshot);

        if (!significance.IsSignificant)
        {
            await db.SaveChangesAsync(ct);

            return new DetectionResult(
                DetectionVerdict.Noise);
        }

        var change = new Change
        {
            Material = material,
            RunId = runId,
            Kind = ChangeKind.Updated,
            PrevSnapshotId = baselineSnapshot.Id,
            NewSnapshot = snapshot,
            Status = ChangeStatus.Pending,
            CreatedAt = now
        };

        db.Changes.Add(change);

        await db.SaveChangesAsync(ct);

        return new DetectionResult(
            DetectionVerdict.Updated,
            change.Id,
            significance.AddedFragments);
    }

    private static Snapshot CreateSnapshot(
        Material material,
        long runId,
        ParsedItem item,
        DateTime now)
    {
        return new Snapshot
        {
            Material = material,
            RunId = runId,
            FetchedAt = now,
            RawText = item.RawText,
            CleanText = item.CleanText,
            ContentHash = item.ContentHash
        };
    }
}