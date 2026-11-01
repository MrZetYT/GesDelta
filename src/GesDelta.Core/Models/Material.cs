namespace GesDelta.Core.Models;

public class Material
{
    public long Id { get; set; }
    public Source? Source { get; set; }
    public long SourceId { get; set; }
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public required string Url  { get; set; }
    public DateTime? PublishedAt { get; set; }
    public required DateTime FirstSeenAt { get; set; }
    public required DateTime LastSeenAt { get; set; }
}