namespace GesDelta.Core.Models;

public class Topic
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public TimeSpan CheckInterval { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}