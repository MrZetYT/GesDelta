namespace GesDelta.Core.Models;

public class ClaimEvidence
{
    public long Id { get; set; }
    public Claim? Claim { get; set; }
    public long ClaimId { get; set; }
    public Snapshot? Snapshot { get; set; }
    public long SnapshotId { get; set; }
    public required string QuoteOriginal {get; set;}
    public string? QuoteRu { get; set; }
}