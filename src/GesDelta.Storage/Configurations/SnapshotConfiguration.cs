using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class SnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder)
    {
        builder.ToTable("snapshots").HasKey(t => t.Id);
        builder.HasOne(t=>t.Material).WithMany().HasForeignKey(t => t.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.Run).WithMany().HasForeignKey(t=>t.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.MaterialId, t.FetchedAt }).IsDescending(false, true);
    }
}