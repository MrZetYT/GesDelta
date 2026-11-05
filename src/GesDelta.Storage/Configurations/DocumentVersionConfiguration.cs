using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_versions").HasKey(t => t.Id);
        builder.HasOne(t => t.Document).WithMany().HasForeignKey(t => t.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t=>t.Run).WithMany().HasForeignKey(t => t.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new {t.DocumentId, t.VersionNo}).IsUnique();
    }
}