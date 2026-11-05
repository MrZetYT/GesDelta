namespace GesDelta.Core.Models;

public class DocumentVersion
{
    public long Id { get; set; }
    public Document? Document { get; set; }
    public long DocumentId { get; set; }
    public int VersionNo { get; set; }
    public required string FilePath { get; set; }
    public required string FileHash { get; set; }
    public Run? Run { get; set; }
    public long RunId { get; set; }
    public required DateTime CreatedAt { get; set; }
}