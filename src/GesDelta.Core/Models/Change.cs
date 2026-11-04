using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public class Change
{
    public long Id { get; set; }
    public Run? Run { get; set; }
    public long RunId { get; set; }
    public Material? Material { get; set; }
    public long MaterialId { get; set; }
    public ChangeKind Kind { get; set; }
    public Snapshot? PrevSnapshot { get; set; }
    public long? PrevSnapshotId { get; set; }
    public Snapshot? NewSnapshot { get; set; }
    public long NewSnapshotId { get; set; }
    public string? Summary { get; set; }
    public ChangeStatus Status { get; set; } = ChangeStatus.Pending;
    public string? Error { get; set; }
    public DateTime? DoneAt { get; set; }
    public required DateTime CreatedAt { get; set; }
}