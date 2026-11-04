namespace GesDelta.Core.Models;

public class Snapshot
{
    public long Id { get; set; }
    public Material? Material { get; set; }
    public long MaterialId { get; set; }
    public Run? Run { get; set; }
    public long RunId { get; set; }
    public required DateTime FetchedAt { get; set; }
    public required string RawText { get; set; }
    public required string CleanText { get; set; }
    public required string ContentHash { get; set; }
}