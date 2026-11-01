using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public class Run
{
    public long Id { get; set; }
    public Topic? Topic { get; set; }
    public long TopicId { get; set; }
    public RunTrigger Trigger { get; set; }
    public RunStatus Status { get; set; }
    public string Stats { get; set; } = "{}";
    public required DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int TokensUsed { get; set; }
    public string? Error { get; set; }
}