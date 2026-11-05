using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims").HasKey(t => t.Id);
        builder.HasOne(t=>t.Topic).WithMany().HasForeignKey(t => t.TopicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.CreatedByChange).WithMany().HasForeignKey(t => t.CreatedByChangeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(t=>t.Id).ValueGeneratedNever();
    }
}