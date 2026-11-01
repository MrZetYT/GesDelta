using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class RunConfiguration : IEntityTypeConfiguration<Run>
{
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        builder.ToTable("runs").HasKey(t => t.Id);
        builder.Property(t => t.StartedAt).IsRequired();
        builder.Property(t => t.Trigger).HasConversion<string>();
        builder.Property(t => t.Status).HasConversion<string>();
        builder.HasOne(t=>t.Topic).WithMany().HasForeignKey(t => t.TopicId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(t => t.Stats).HasColumnType("jsonb");
        builder.HasIndex(t=> t.TopicId).IsUnique().HasFilter("status = 'Running'");
        builder.HasIndex(t => new { t.TopicId, t.StartedAt }).IsDescending(false, true);
    }
}