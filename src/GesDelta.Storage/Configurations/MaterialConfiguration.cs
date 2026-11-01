using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials").HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired();
        builder.Property(t => t.ExternalId).IsRequired();
        builder.Property(t => t.Url).IsRequired();
        builder.Property(t => t.FirstSeenAt).IsRequired();
        builder.Property(t => t.LastSeenAt).IsRequired();
        builder.HasOne(t=>t.Source).WithMany().HasForeignKey(t => t.SourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new {t.SourceId, t.ExternalId}).IsUnique();
    }
}