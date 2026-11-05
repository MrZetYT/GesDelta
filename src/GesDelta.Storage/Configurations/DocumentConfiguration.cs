using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents").HasKey(t => t.Id);
        builder.HasOne(t => t.Topic).WithMany().HasForeignKey(t => t.TopicId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(t=>t.Kind).HasConversion<String>();
    }
}