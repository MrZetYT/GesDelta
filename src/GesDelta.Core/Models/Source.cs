using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public class Source
{
    public long Id { get; set; }
    public Topic? Topic { get; set; }
    public long TopicId { get; set; }
    public required string Name { get; set; }
    public SourceType Type { get; set; }
    public required string Url { get; set; }
    public string Config { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
}