using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class ChangeConfiguration : IEntityTypeConfiguration<Change>
{
    public void Configure(EntityTypeBuilder<Change> builder)
    {
        builder.ToTable("changes").HasKey(t => t.Id);
        builder.Property(t => t.Kind).HasConversion<string>();
        builder.Property(t => t.Status).HasConversion<string>();
        builder.HasOne(t=>t.Material).WithMany().HasForeignKey(t => t.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.Run).WithMany().HasForeignKey(t=>t.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.PrevSnapshot).WithMany().HasForeignKey(t => t.PrevSnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.NewSnapshot).WithMany().HasForeignKey(t => t.NewSnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.MaterialId, t.Id }).IsDescending(false, true);
        builder.HasIndex(t=> t.Status).HasFilter("status = 'Pending'");
        builder.HasIndex(t => t.RunId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.DoneAt);
    }
}