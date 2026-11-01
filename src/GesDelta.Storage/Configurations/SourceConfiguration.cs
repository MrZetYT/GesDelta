using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("sources").HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Type).HasConversion<string>();
        builder.HasOne(t=>t.Topic).WithMany().HasForeignKey(t => t.TopicId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(t => t.Config).HasColumnType("jsonb");
    }
}