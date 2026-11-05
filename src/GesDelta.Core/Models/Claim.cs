namespace GesDelta.Core.Models;

public class Claim
{
    public long Id { get; set; }
    public Topic? Topic { get; set; }
    public long TopicId { get; set; }
    public required string Text { get; set; }
    public Change? CreatedByChange { get; set; }
    public long CreatedByChangeId { get; set; }
    public required DateTime CreatedAt { get; set; }
}