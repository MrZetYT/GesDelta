using GesDelta.Core.Models;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;

namespace GesDelta.Agents;

public enum SaveOutcome { NewMaterial, NewSnapshot, Unchanged }

public class TemporaryMaterialWriter(GesDeltaDbContext db, TimeProvider time)
{
    public async Task<SaveOutcome> SaveAsync(
        long sourceId, long runId, ParsedItem item, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;

        var material = await db.Materials.FirstOrDefaultAsync(
            m => m.SourceId == sourceId && m.ExternalId == item.ExternalId, ct);

        if (material is null)
        {
            var newMaterial = new Material
            {
                SourceId = sourceId,
                ExternalId = item.ExternalId,
                Title = item.Title,
                Url = item.Url,
                PublishedAt = item.PublishedAt,
                FirstSeenAt = now,
                LastSeenAt = now
            };

            var newSnapshot = new Snapshot
            {
                Material = newMaterial,
                RunId = runId,
                FetchedAt = now,
                RawText = item.RawText,
                CleanText = item.CleanText,
                ContentHash = item.ContentHash
            };
            
            db.Add(newMaterial);
            db.Add(newSnapshot);
            
            await db.SaveChangesAsync(ct);
            
            return SaveOutcome.NewMaterial;
        }

        var lastHash = await db.Snapshots
            .Where(s => s.MaterialId == material.Id)
            .OrderByDescending(s => s.FetchedAt)
            .ThenByDescending(s => s.Id)
            .Select(s => s.ContentHash)
            .FirstAsync(ct);

        material.LastSeenAt = now;

        if (lastHash == item.ContentHash)
        {
            await db.SaveChangesAsync(ct);
            return SaveOutcome.Unchanged;
        }

        var newSnapshotWithoutNewMaterial = new Snapshot
        {
            MaterialId = material.Id,
            RunId = runId,
            FetchedAt = now,
            CleanText = item.CleanText,
            RawText = item.RawText,
            ContentHash = item.ContentHash
        };
        
        db.Add(newSnapshotWithoutNewMaterial);

        await db.SaveChangesAsync(ct);
        
        return SaveOutcome.NewSnapshot;
    }
}