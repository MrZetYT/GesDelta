using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GesDelta.Storage.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topics").HasKey(t => t.Name);
        builder.Property(t => t.Name).IsRequired();
    }
}