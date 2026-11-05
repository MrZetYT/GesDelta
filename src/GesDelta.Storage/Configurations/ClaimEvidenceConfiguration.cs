using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class ClaimEvidenceConfiguration : IEntityTypeConfiguration<ClaimEvidence>
{
    public void Configure(EntityTypeBuilder<ClaimEvidence> builder)
    {
        builder.ToTable("claim_evidence").HasKey(t => t.Id);
        builder.HasOne(t => t.Claim).WithMany().HasForeignKey(t => t.ClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Snapshot).WithMany().HasForeignKey(t => t.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ClaimId);
    }
}